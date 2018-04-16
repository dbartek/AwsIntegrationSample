using AmazonWebApp.Amazon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmazonWebApp.Workers
{
    public class PictureTransformWorker
    {
        private AmazonClient _amazonClient;

        public PictureTransformWorker(AmazonClient amazonClient)
        {
            _amazonClient = amazonClient;
        }

        public void RunTransformations()
        {
            _amazonClient.TransformFiles();
        }
    }
}
