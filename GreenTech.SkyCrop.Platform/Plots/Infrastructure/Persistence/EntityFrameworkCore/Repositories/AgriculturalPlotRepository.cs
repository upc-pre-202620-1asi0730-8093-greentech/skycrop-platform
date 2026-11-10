using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Plots.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace GreenTech.SkyCrop.Platform.Plots.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public sealed class AgriculturalPlotRepository(AppDbContext context)
    : BaseRepository<AgriculturalPlot>(context), IAgriculturalPlotRepository { }
