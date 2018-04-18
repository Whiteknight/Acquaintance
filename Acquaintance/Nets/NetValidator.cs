using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.Nets
{
    public class NetValidator
    {
        private static readonly INetValidation[] _validations = {
            new HasAnyChannelsNetValidation(),
            new AllInputChannelsExistNetValidation(),
            new AllNodesHaveOutputsNetValidation()
        };

        public void Validate(Dictionary<string, IReadOnlyList<NodeChannel>> inputs, Dictionary<string, IReadOnlyList<NodeChannel>> outputs)
        {
            var context = new NetValidationContext(inputs, outputs);
            foreach (var validation in _validations)
                validation.Check(context);

            if (context.HasErrors)
                throw new NetValidationException(context.ToString());
        }
    }

    public interface INetValidation
    {
        void Check(NetValidationContext context);
    }

    public class HasAnyChannelsNetValidation : INetValidation
    {
        public void Check(NetValidationContext context)
        {
            if (context.Inputs.Count == 0)
                context.AddError("Must have at least one node");
            if (context.Outputs.Count == 0)
                context.AddError("Nodes generate no outputs");
        }
    }

    public class AllNodesHaveOutputsNetValidation : INetValidation
    {
        public void Check(NetValidationContext context)
        {
            foreach (var output in context.Outputs.Where(o => o.Value.Count == 0))
                context.AddError($"Node {output.Key} has no output channels");
        }
    }

    public class AllInputChannelsExistNetValidation : INetValidation
    {
        public void Check(NetValidationContext context)
        {
            foreach (var input in context.Inputs)
            {
                if (input.Value.Count == 0)
                    context.AddError($"Node {input.Key} has no input channels defined");
                foreach (var channel in input.Value)
                {
                    if (channel.Topic == Net.NetworkInputTopic)
                        continue;
                    var channelName = channel.ToString();
                    if (!context.AllOutputs.Contains(channelName))
                        context.AddError($"Node {input.Key} reads from channel {channelName} but no nodes write to that channel");
                }
            }
        }
    }
}