using Gw2Gizmos.Gw2Api.Contract.V2.Skills;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Skills;

public class SkillsClient : BaseBulkAllClient<Skill, int>, ISkillsClient
{
    internal SkillsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/skills";
}
