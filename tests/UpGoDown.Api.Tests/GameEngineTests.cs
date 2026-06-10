using Xunit;
using UpGoDown.Api.Services;

namespace UpGoDown.Api.Tests;

public class GameEngineTests
{
    private static readonly string[] Level1DemoCommands =
    [
        "встать", "идти", "идти", "идти", "идти", "идти", "идти", "идти",
        "повернуть_90", "идти", "повернуть_90", "идти", "повернуть_90", "идти", "идти",
        "повернуть_-90", "идти", "идти", "идти", "идти", "идти", "идти", "идти",
        "повернуть_-90", "идти", "сесть",
    ];

    [Fact]
    public void Level1_DemoCommands_ReturnsSuccess()
    {
        var engine = new GameEngine();
        var result = engine.Run(new SimulationInput
        {
            GridWidth = 14,
            GridHeight = 8,
            ChairOwnX = 2,
            ChairOwnY = 4,
            ChairPartnerX = 9,
            ChairPartnerY = 4,
            SpawnX = 2,
            SpawnY = 4,
            SpawnAngle = 0,
            SittingAtStart = true,
            LevelId = 1,
            Commands = Level1DemoCommands.ToList(),
        });

        Assert.True(result.Success);
        Assert.Equal(26, result.StepsCount);
        Assert.True(result.Lines!.Yellow);
        Assert.True(result.Lines.Orange);
        Assert.True(result.Lines.Green);
    }

    [Fact]
    public void Level3_EnemyAttack_ReducesHp()
    {
        var engine = new GameEngine();
        var result = engine.Run(new SimulationInput
        {
            GridWidth = 14,
            GridHeight = 8,
            ChairOwnX = 2,
            ChairOwnY = 4,
            ChairPartnerX = 9,
            ChairPartnerY = 4,
            SpawnX = 5,
            SpawnY = 5,
            SpawnAngle = 0,
            SittingAtStart = false,
            LevelId = 3,
            EnemySpawnX = 4,
            EnemySpawnY = 5,
            ActorHp = 3,
            Commands = ["повернуть_90"],
        });

        Assert.Equal(2, result.ActorHp);
    }

    [Fact]
    public void DiagonalSkill_WithoutSkill_FailsOnDiagonalCommand()
    {
        var engine = new GameEngine();
        var result = engine.Run(new SimulationInput
        {
            GridWidth = 14,
            GridHeight = 8,
            ChairOwnX = 2,
            ChairOwnY = 4,
            ChairPartnerX = 9,
            ChairPartnerY = 4,
            SpawnX = 2,
            SpawnY = 4,
            SpawnAngle = 0,
            SittingAtStart = false,
            DiagonalSkillEnabled = false,
            Commands = ["идти_диагональ"],
        });

        Assert.False(result.Success);
        Assert.Contains("идти_диагональ", result.Error ?? "");
    }
}
