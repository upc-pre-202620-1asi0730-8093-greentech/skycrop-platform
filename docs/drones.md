# Drone management backend

The `Drones` bounded context follows the structure of `learning-center-platform`:

- **Domain:** aggregates, immutable measurements, commands, queries and repository contracts.
- **Application:** command services enforce lifecycle rules; query services read without tracking.
- **Infrastructure:** EF Core repositories, MySQL mappings and a context-specific unit of work.
- **Interfaces:** REST resources, resource/command assemblers and controllers.

The contract was checked against `mock-api/db.json` and the existing web app's
`src/drones` module. Runtime persistence uses MySQL; the backend does not call the
fake API or read `db.json`. `tests/fixtures/drones-mock-contract.json` contains only
the four relevant collections for reproducible contract checks, without user data.

## Run

Requirements: .NET 10 and a reachable MySQL 8+ database. Configure a database user
with permission to create/apply the schema in the development database:

```sh
export ConnectionStrings__DefaultConnection='server=127.0.0.1;port=3306;database=skycrop;user=YOUR_USER;password=YOUR_PASSWORD;'
export ASPNETCORE_ENVIRONMENT=Development
dotnet build
dotnet run --project GreenTech.SkyCrop.Platform --no-launch-profile --urls http://localhost:5197
```

The application's existing startup migration step applies the Drones migrations.
The first migration creates the schema; the second explicitly applies binary
comparison to canonical serial keys because this MySQL provider does not emit
property-level collation in its generated column DDL. Credentials belong in
configuration overrides or a secret store, not source control.

Development API documentation is available at `/scalar/v1` and `/openapi/v1.json`.

## Endpoints

All paths below have the `/api/v1` prefix. Responses contain plain JSON objects
or arrays, as expected by the existing API clients. Creation returns 201 and a
`Location` header; updates return 200. Invalid input returns 400, missing resources
404, and conflicting state or concurrent writes 409, using problem details.

| Method | Path | Operation |
|---|---|---|
| GET | `/drones` | List drones. |
| GET | `/drones/{id}` | Read a drone. |
| POST | `/drones` | Register a disconnected drone. |
| PATCH | `/drones/{id}` | Update drone information or logical connection state. |
| GET | `/flightRoutes` | List routes with ordered waypoints. |
| GET | `/flightRoutes/{id}` | Read a route. |
| POST | `/flightRoutes` | Create a route. |
| PATCH | `/flightRoutes/{id}` | Update supplied route fields. |
| GET | `/flights` | List flights. |
| GET | `/flights/{id}` | Read a flight. |
| POST | `/flights` | Plan a flight for an available drone and existing route. |
| PATCH | `/flights/{id}` | Cancel a planned flight. |
| GET | `/aerialImages` | List persisted image metadata. |
| GET | `/aerialImages/{id}` | Read image metadata. |

`/flight-routes` and `/aerial-images` are equivalent aliases for the backend's
kebab-case convention. The existing frontend collection paths remain supported.
Empty lists return `[]`. PATCH only changes supplied fields, rejects empty bodies,
and rejects unknown resource members. Null optional PATCH values are treated as
omitted; use `imageUrl: ""` to clear an optional drone image.

### Register and connect a drone

```json
{
  "name": "Field drone",
  "model": "DJI Agras T30",
  "serialNumber": "SK-391-492",
  "camera": "48 MP FR",
  "autonomyMinutes": 30,
  "battery": 100,
  "status": "DISCONNECTED",
  "imageUrl": ""
}
```

Text fields are trimmed and limited to 120 characters. Serial uniqueness ignores
case and surrounding whitespace; accent differences remain significant. Battery
is in `[0,100]`; autonomy is positive and expressed in minutes. Optional image URLs
must use HTTP or HTTPS. Decimal measurements support six fractional digits and a
maximum of `999999999999.999999` to match SQL storage; device-specific operating
limits require their own policy. No universal legal flight limit is assumed.

Connect with `PATCH /drones/{id}` and `{"status":"AVAILABLE"}`. Disconnect with
`{"status":"DISCONNECTED"}`. These operations record application state; they do
not establish a physical link to drone hardware. `ON_MISSION` and `MAINTENANCE`
remain readable stored states, but cannot be entered through this connection
endpoint. The client cannot create a drone as already on a mission.

### Create a route

```json
{
  "name": "Route 1",
  "plotId": 3,
  "altitudeMeters": 30,
  "speedMetersPerSecond": 4.4,
  "waypoints": [
    {"latitude": -11.380296, "longitude": -73.922818},
    {"latitude": -11.388121, "longitude": -73.920757}
  ]
}
```

A route requires a positive plot identifier, positive altitude and speed, and at
least two geographic points. Latitude must be within `[-90,90]`, longitude within
`[-180,180]`. Their order is preserved. Active flight routes cannot be edited.

### Plan and cancel a flight

```json
{
  "droneId": 1,
  "flightRouteId": 1,
  "plotId": 3,
  "status": "PLANNED"
}
```

The drone and route must exist, the drone must be `AVAILABLE`, and `plotId` must
match the route. Only one active flight is allowed per drone. A nullable unique
index and optimistic concurrency tokens protect this rule against simultaneous
requests, including a connection change racing with flight planning. Successful
cancellation releases the reservation.

Cancel using `PATCH /flights/{id}` with `{"status":"CANCELLED"}`. Only a planned
flight can be cancelled; repeat cancellation returns 409. Starting or completing
real flights is outside the current UI contract and is not simulated here.

`createdAt` is accepted on creation for compatibility with the frontend assembler,
but the server assigns the stored timestamp. IDs are generated by the database.
Flight resources also expose nullable `startedAt` and `durationMinutes` for the
existing flight history view. Image resources expose `imageUrl`, as consumed by
the frontend's files tab, while the persistence model stores `file_url`.

## Cross-context integration

`plotId` is a reference to the Agricultural Plot context. This implementation
validates the identifier and route/flight consistency; it does not create a plot
table or validate plot existence and access rights on behalf of that context.
Those checks must be connected to its public contract when that module is ready.
The current backend skeleton has no IAM middleware; ownership and authorization
must be integrated through the IAM context rather than inferred from fake API data.

The Drones frontend calls `/plots` while loading its views. Switching its single
shared API base URL requires that endpoint and the other relevant contexts to be
available. This change deliberately leaves frontend configuration and the fake API
untouched. No hardware telemetry, image upload or image analysis is fabricated.

## Checks

Domain invariants, response shape and EF mapping checks run without a database:

```sh
dotnet run --project tests/SkyCrop.Drones.Checks
```

Run the HTTP lifecycle and concurrency suite against a **disposable development
database**. It creates records and keeps them for the persistence check:

```sh
python3 tests/check_drones_http.py --state-file /tmp/skycrop-drones-state.json
```

Restart the backend with the same connection string, then verify stored records:

```sh
python3 tests/check_drones_http.py --state-file /tmp/skycrop-drones-state.json --verify-persistence
```

Both commands accept `--base-url http://localhost:5197/api/v1`. The .NET checks can
also persist/read image metadata for an existing test flight by explicitly setting
`SKYCROP_TEST_CONNECTION` and `SKYCROP_TEST_FLIGHT_ID`. This is a test-only data path;
there is no image-capture REST endpoint.
