using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Acquaintance.Nets
{
    public class NetValidationContext
    {
        private readonly List<string> _errors;

        public IReadOnlyDictionary<string, IReadOnlyList<NodeChannel>> Inputs { get; }
        public IReadOnlyDictionary<string, IReadOnlyList<NodeChannel>> Outputs { get; }
        public IReadOnlyCollection<string> AllOutputs { get; }

        public NetValidationContext(IReadOnlyDictionary<string, IReadOnlyList<NodeChannel>> inputs, IReadOnlyDictionary<string, IReadOnlyList<NodeChannel>> outputs)
        {
            Inputs = inputs;
            Outputs = outputs;
            _errors = new List<string>();
            AllOutputs = (IReadOnlyCollection<string>)new HashSet<string>(outputs.SelectMany(kvp => kvp.Value).Select(c => c.ToString()).Distinct());
        }

        public IReadOnlyList<string> GetErrors()
        {
            return _errors;
        }

        public void AddError(string error)
        {
            if (string.IsNullOrEmpty(error))
                return;
            _errors.Add(error);
        }

        public bool HasErrors => _errors.Any();

        public override string ToString()
        {
            if (!HasErrors)
                return string.Empty;
            var sb = new StringBuilder();
            foreach (var error in _errors)
                sb.AppendLine(error);
            return sb.ToString();
        }
    }
}