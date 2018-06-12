using System;
using System.Collections.Generic;

namespace Acquaintance.Routing
{
    public interface IFilterRouteBuilderSingleInput<out T>
    {
        /// <summary>
        /// Specify the input topic to route
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        IFilterRouteBuilderWhen<T> FromTopic(string topic);

        /// <summary>
        /// Use the default topic
        /// </summary>
        /// <returns></returns>
        IFilterRouteBuilderWhen<T> FromDefaultTopic();
    }

    public interface IFilterRouteBuilderMultiInput<out T>
    {
        /// <summary>
        /// Specify a list of input topics to route
        /// </summary>
        /// <param name="topics"></param>
        /// <returns></returns>
        IFilterRouteBuilderWhen<T> FromTopics(params string[] topics);

        /// <summary>
        /// Specify a list of input topics to route
        /// </summary>
        /// <param name="topics"></param>
        /// <returns></returns>
        IFilterRouteBuilderWhen<T> FromTopics(IEnumerable<string> topics);

        /// <summary>
        /// Route from the default topic
        /// </summary>
        /// <returns></returns>
        IFilterRouteBuilderWhen<T> FromDefaultTopic();
    }

    public interface IFilterRouteBuilderWhen<out T>
    {
        /// <summary>
        /// Route the message to the provided output topic when the predicate is satisfied.
        /// Only the first matching predicate is used for routing
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="topic"></param>
        /// <returns></returns>
        IFilterRouteBuilderWhen<T> When(Func<T, bool> predicate, string topic);

        /// <summary>
        /// Route the message to the provided default topic when no When predicates are met
        /// </summary>
        /// <param name="defaultRoute"></param>
        void Else(string defaultRoute);
    }
}
