using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lever.Common.Models
{
    public class ValidationResults
    {
        public ValidationResults()
        {
            Issues = new List<Issue>();
            ValidApplications = new List<string>();
            Actions = new List<Action>();
        }
        public List<Issue> Issues { get; set; }
        public List<string> ValidApplications { get; set; }
        public List<Action> Actions { get; set; }
    }
}
