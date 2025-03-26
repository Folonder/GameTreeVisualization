using GameTreeVisualization.Core.Models.Tree;
using GameTreeVisualization.Infrastructure.Models.Redis;


public class TreeMapper
{
    public TreeNode MapToTreeNode(RedisNode redisNode, int depth = 0)
    {
        if (redisNode == null) return null;

        var treeNode = new TreeNode
        {
            Id = Guid.NewGuid().ToString(),
            State = redisNode.State != null ? string.Join(", ", redisNode.State) : "",
            Depth = depth,
            Children = new List<TreeNode>(),
            Statistics = new NodeStatistics
            {
                NumVisits = redisNode.Statistics?.NumVisits ?? 0,
                RelativeVisits = 0,
                StatisticsForActions = MapRoleStatistics(redisNode)
            }
        };

        if (redisNode.Children != null)
        {
            foreach (var childNode in redisNode.Children)
            {
                var child = MapToTreeNode(childNode, depth + 1);
                if (child != null)
                {
                    treeNode.Children.Add(child);
                }
            }
        }

        RecalculateRelativeVisits(treeNode);
        return treeNode;
    }

    private List<RoleStatistics> MapRoleStatistics(RedisNode redisNode)
    {
        var roleStats = new List<RoleStatistics>();

        if (redisNode.Statistics?.StatisticsForActions?.Map != null &&
            redisNode.Statistics.StatisticsForActions.Roles != null)
        {
            foreach (var roleInfo in redisNode.Statistics.StatisticsForActions.Roles)
            {
                string roleName = roleInfo.Name?.Value ?? "";

                if (redisNode.Statistics.StatisticsForActions.Map.TryGetValue(roleName, out var actionMap))
                {
                    var roleStatistics = new RoleStatistics
                    {
                        Role = roleName,
                        Actions = actionMap.Select(actionEntry => new ActionStatistics
                        {
                            Action = actionEntry.Key,
                            AverageActionScore = actionEntry.Value.ActionScore,
                            ActionNumUsed = actionEntry.Value.ActionNumUsed
                        }).ToList()
                    };

                    roleStats.Add(roleStatistics);
                }
            }
        }

        return roleStats;
    }

    private void RecalculateRelativeVisits(TreeNode node)
    {
        if (node?.Children == null || !node.Children.Any()) return;

        int totalChildVisits = node.Children.Sum(c => c.Statistics?.NumVisits ?? 0);

        foreach (var child in node.Children)
        {
            if (child.Statistics != null && totalChildVisits > 0)
            {
                child.Statistics.RelativeVisits = (double)(child.Statistics.NumVisits * 100) / totalChildVisits;
            }

            RecalculateRelativeVisits(child);
        }
    }
}