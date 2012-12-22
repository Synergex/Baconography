using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface IImagesService
    {
        Task<IEnumerable<Tuple<string, string>>> GetImagesFromUrl(string title, string url);
        bool MightHaveImagesFromUrl(string url);
        Task<object> GenerateResizedImage(object inputFile, uint width, uint height, uint edgePadding = 5, uint bottomPadding = 20, bool replaceIfExists = true);
        Task<object> SaveFileFromUriAsync(Uri fileUri, string localFileName, string localPath = "Images", bool replaceIfExists = true);
    }
}
