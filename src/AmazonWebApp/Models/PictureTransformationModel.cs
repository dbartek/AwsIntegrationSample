using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmazonWebApp.Models
{
    public class PicturesTransformationModel
    {
        public List<string> FileNames { get; set; }
        public Transformation Transformation { get; set; }
    }

    public enum Transformation
    {
        Rotate
    }
}
