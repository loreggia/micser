using System;
using System.Collections.Generic;
using Unity.Builder;
using Unity.Injection;
using Unity.Policy;
using Unity.Resolution;

namespace Micser.Common
{
    /// <summary>
    /// Unity override to allow passing of parameters without having to specify the names during dependency resolution.
    /// </summary>
    public class OrderedParametersOverride : ResolverOverride
    {
        private readonly Queue<InjectionParameterValue> _parameterValues;

        /// <summary>
        /// Creates an instance of the <see cref="OrderedParametersOverride"/> class.
        /// </summary>
        /// <param name="parameterValues">The values to pass to the resolved object's constructor in the passed order.</param>
        public OrderedParametersOverride(params object[] parameterValues)
        {
            _parameterValues = new Queue<InjectionParameterValue>();

            foreach (var parameterValue in parameterValues)
            {
                _parameterValues.Enqueue(InjectionParameterValue.ToParameter(parameterValue));
            }
        }

        /// <inheritdoc />
        public override IResolverPolicy GetResolver(IBuilderContext context, Type dependencyType)
        {
            if (_parameterValues.Count < 1)
            {
                return null;
            }

            var value = _parameterValues.Dequeue();
            return value.GetResolverPolicy(dependencyType);
        }
    }
}