using File = Gw2Gizmos.Gw2Api.Contract.Files.File;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Files;

public interface IFilesClient : IAllExpandableClient<File>, IBulkExpandableClient<File, string>, IPaginatedClient<File>;
