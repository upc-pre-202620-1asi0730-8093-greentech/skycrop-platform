#!/usr/bin/env python3
"""Exercise a disposable backend database through the real frontend REST contract.

Run only against a development instance: records are deliberately retained for a
second invocation with --verify-persistence, after restarting the backend.
"""
import argparse
import concurrent.futures
import json
from pathlib import Path
import urllib.error
import urllib.request
import uuid

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--base-url', default='http://127.0.0.1:5197/api/v1')
parser.add_argument('--state-file', required=True)
parser.add_argument('--verify-persistence', action='store_true')
args = parser.parse_args()
base = args.base_url.rstrip('/')
count = 0

def request(method, path, payload=None):
    data = None if payload is None else json.dumps(payload).encode()
    req = urllib.request.Request(base + path, data=data, method=method,
                                 headers={'Content-Type': 'application/json'})
    try:
        with urllib.request.urlopen(req, timeout=20) as response:
            return response.status, json.load(response), response.headers
    except urllib.error.HTTPError as failure:
        raw = failure.read()
        return failure.code, json.loads(raw) if raw else {}, failure.headers

def check(condition, message):
    global count
    if not condition:
        raise AssertionError(message)
    count += 1
    print('PASS', message, flush=True)

def expect(method, path, payload, status):
    response = request(method, path, payload)
    check(response[0] == status, f'{method} {path}: expected {status}, received {response[0]} {response[1] if response[0] != status else ""}')
    if status in (400, 404, 409):
        check(response[1].get('status') == status, 'failure is a problem details document')
    return response

if args.verify_persistence:
    state = json.loads(Path(args.state_file).read_text())
    drone = expect('GET', '/drones/' + str(state['droneId']), None, 200)[1]
    check(drone['serialNumber'] == state['serialNumber'], 'drone survives process restart')
    route = expect('GET', '/flightRoutes/' + str(state['routeId']), None, 200)[1]
    check(route['waypoints'] == state['waypoints'], 'ordered waypoints survive process restart')
    flight = expect('GET', '/flights/' + str(state['flightId']), None, 200)[1]
    check(flight['status'] == 'CANCELLED', 'cancelled flight survives process restart')
    images = expect('GET', '/aerialImages', None, 200)[1]
    image = next((item for item in images if item['flightId'] == state['flightId']), None)
    if image:
        check(image['imageUrl'] == 'https://example.com/drones-check-image.jpg', 'persisted image URL matches the frontend contract')
        expect('GET', '/aerialImages/' + str(image['id']), None, 200)
    print(f'Completed {count} persistence checks.')
    raise SystemExit(0)

fixture = json.loads((Path(__file__).parent / 'fixtures/drones-mock-contract.json').read_text())
suffix = uuid.uuid4().hex[:12]
drone_input = {k: v for k, v in fixture['drones'][0].items() if k != 'id'}
drone_input.update(serialNumber='CHECK-' + suffix, status='DISCONNECTED',
                   createdAt='1900-01-01T00:00:00Z')
for path in ['/drones', '/flightRoutes', '/flights', '/aerialImages', '/flight-routes', '/aerial-images']:
    check(isinstance(expect('GET', path, None, 200)[1], list), path + ' returns a plain JSON array')
created = expect('POST', '/drones', drone_input, 201)
drone = created[1]
drone_id = drone['id']
check(set(drone) == set(fixture['drones'][0]), 'drone response has the same fields as mock db.json')
check(drone['status'] == 'DISCONNECTED' and not drone['createdAt'].startswith('1900'), 'registration state and timestamps are server controlled')
with urllib.request.urlopen(base.split('/api/v1')[0] + created[2]['Location'] if created[2]['Location'].startswith('/') else created[2]['Location']) as response:
    check(response.status == 200, 'created resource Location resolves')
expect('POST', '/drones', dict(drone_input, serialNumber='  ' + drone_input['serialNumber'].lower() + '  '), 409)
for patch in [{'battery': -1}, {'battery': 101}, {'autonomyMinutes': 0}, {'imageUrl': 'javascript:bad'}, {'serialNumber': ' '}, {'status': 'unknown'}, {'battery': 0.1234567}]:
    expect('PATCH', f'/drones/{drone_id}', patch, 400)
expect('PATCH', f'/drones/{drone_id}', {}, 400)
expect('PATCH', f'/drones/{drone_id}', {'createdAt': '1900-01-01T00:00:00Z'}, 400)
expect('PATCH', f'/drones/{drone_id}', {'status': 'ON_MISSION'}, 409)
check(expect('GET', f'/drones/{drone_id}', None, 200)[1]['battery'] == 100, 'failed updates do not corrupt stored data')
expect('GET', '/drones/2147483647', None, 404)
expect('PATCH', '/drones/2147483647', {'status': 'AVAILABLE'}, 404)
route_input = {k: v for k, v in fixture['flightRoutes'][0].items() if k != 'id'}
route_input['name'] = 'Route ' + suffix
for bad_points in [[], [{'latitude': 0, 'longitude': 0}], [{'latitude': 91, 'longitude': 0}, {'latitude': 0, 'longitude': 0}], [None, {'latitude': 0, 'longitude': 0}], [{'longitude': 0}, {'latitude': 0, 'longitude': 0}]]:
    expect('POST', '/flightRoutes', dict(route_input, waypoints=bad_points), 400)
expect('POST', '/flightRoutes', dict(route_input, speedMetersPerSecond=0), 400)
route = expect('POST', '/flightRoutes', route_input, 201)[1]
route_id = route['id']
check(route['waypoints'] == route_input['waypoints'], 'route preserves coordinates and waypoint order')
expect('PATCH', f'/flightRoutes/{route_id}', {'name': 'Updated ' + suffix}, 200)
updated_points = [{'latitude': -12.1, 'longitude': -76.1}, {'latitude': -12.2, 'longitude': -76.2}, {'latitude': -12.3, 'longitude': -76.3}]
check(expect('PATCH', f'/flightRoutes/{route_id}', {'waypoints': updated_points}, 200)[1]['waypoints'] == updated_points, 'owned waypoints can be replaced and shortened')
expect('PATCH', f'/flightRoutes/{route_id}', {}, 400)
flight_input = {'droneId': drone_id, 'flightRouteId': route_id, 'plotId': route['plotId'], 'status': 'PLANNED', 'createdAt': '1900-01-01T00:00:00Z'}
expect('POST', '/flights', flight_input, 409)
expect('PATCH', f'/drones/{drone_id}', {'status': 'AVAILABLE'}, 200)
expect('POST', '/flights', dict(flight_input, plotId=route['plotId'] + 1), 400)
expect('POST', '/flights', dict(flight_input, status='COMPLETED'), 400)
expect('POST', '/flights', dict(flight_input, flightRouteId=2147483647), 404)
flight = expect('POST', '/flights', flight_input, 201)[1]
flight_id = flight['id']
check(set(fixture['flights'][0]) <= set(flight), 'flight response retains mock contract fields')
expect('POST', '/flights', flight_input, 409)
expect('PATCH', f'/drones/{drone_id}', {'status': 'DISCONNECTED'}, 409)
expect('PATCH', f'/flightRoutes/{route_id}', {'altitudeMeters': 40}, 409)
expect('PATCH', f'/flights/{flight_id}', {'status': 'IN_PROGRESS'}, 400)
expect('PATCH', f'/flights/{flight_id}', {'status': 'CANCELLED'}, 200)
expect('PATCH', f'/flights/{flight_id}', {'status': 'CANCELLED'}, 409)

# Requests intentionally overlap; database constraints and tokens must enforce invariants.
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
    results = list(pool.map(lambda _: request('POST', '/flights', flight_input), range(6)))
check(sum(r[0] == 201 for r in results) == 1 and all(r[0] in (201, 409) for r in results), 'concurrent flight requests create exactly one active flight')
active_id = next(r[1]['id'] for r in results if r[0] == 201)
expect('PATCH', f'/flights/{active_id}', {'status': 'CANCELLED'}, 200)
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
    race_input = dict(drone_input, serialNumber='RACE-' + suffix)
    results = list(pool.map(lambda _: request('POST', '/drones', race_input), range(4)))
check(sum(r[0] == 201 for r in results) == 1 and all(r[0] in (201, 409) for r in results), 'concurrent duplicate serial registration is prevented')
# Changing default database collations must not silently change serial identity.
expect('POST', '/drones', dict(drone_input, serialNumber='Á-' + suffix), 201)
expect('POST', '/drones', dict(drone_input, serialNumber='A-' + suffix), 201)
expect('POST', '/drones', dict(drone_input, serialNumber='á-' + suffix), 409)
# A connection change and a reservation cannot both succeed from the same state.
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
    tasks = [pool.submit(request, 'POST', '/flights', flight_input),
             pool.submit(request, 'PATCH', f'/drones/{drone_id}', {'status': 'DISCONNECTED'})]
    racing = [future.result() for future in tasks]
check(all(result[0] in (200, 201, 409) for result in racing) and not
      (racing[0][0] == 201 and racing[1][0] == 200), 'flight reservation and disconnect cannot both win')
if racing[0][0] == 201:
    expect('PATCH', '/flights/' + str(racing[0][1]['id']), {'status': 'CANCELLED'}, 200)
expect('GET', '/aerialImages/2147483647', None, 404)
check(request('POST', '/aerialImages', {'flightId': flight_id})[0] == 405, 'no simulated image-capture endpoint is exposed')
Path(args.state_file).write_text(json.dumps({'droneId': drone_id, 'serialNumber': drone_input['serialNumber'], 'routeId': route_id, 'waypoints': updated_points, 'flightId': flight_id}))
print(f'Completed {count} HTTP checks. Test records remain in the development database.')
