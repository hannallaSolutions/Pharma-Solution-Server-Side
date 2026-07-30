namespace SearchTool_ServerSide.Dtos.BranchIntelligenceDto
{
    public class BranchIntelligenceOverviewDto
    {
        public List<BranchExecutiveCardDto> ExecutiveCards { get; set; } = new();
        public List<BranchLeaderboardRowDto> Leaderboard { get; set; } = new();
        public List<string> Insights { get; set; } = new();
    }
}
