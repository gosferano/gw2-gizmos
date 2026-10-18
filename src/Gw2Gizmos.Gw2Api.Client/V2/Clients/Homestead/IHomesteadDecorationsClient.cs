using Gw2Gizmos.Gw2Api.Contract.V2.Homestead;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Homestead;

/// <summary>
/// Unlike most bulk endpoints, decorations can't all be fetched at once (<c>ids=all</c>): fetch them by ids or by page.
/// </summary>
public interface IHomesteadDecorationsClient
    : IBulkExpandableClient<HomesteadDecoration, int>,
        IPaginatedClient<HomesteadDecoration>
{
    public IHomesteadDecorationCategoriesClient Categories { get; }
}
