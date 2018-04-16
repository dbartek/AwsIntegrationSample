using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmazonWebApp.Models
{
    public class UploadFileViewModel
    {
        public string UploadUrl { get; set; }
        public string RedirectUrl { get; set; }
        public string AccessKey { get; set; }
        public string Policy { get; set; }
        public string Signature { get; set; }
    }
}
