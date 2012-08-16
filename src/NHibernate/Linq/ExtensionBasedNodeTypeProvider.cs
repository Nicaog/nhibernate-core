using System;
using System.Linq;
using System.Reflection;
using Remotion.Linq.Parsing.Structure;

namespace NHibernate.Linq
{
    [AttributeUsage(AttributeTargets.Method)]
    public class NodeTypeProviderAttribute : Attribute
    {
        public System.Type NodeType { get; private set; }

        public NodeTypeProviderAttribute(System.Type nodeType)
        {
            if (nodeType == null) throw new ArgumentNullException("nodeType");

            NodeType = nodeType;
        }
    }

    /// <summary>
    /// Provides nodes based on an attrbute assigned to the method
    /// </summary>
    public class ExtensionBasedNodeTypeProvider : INodeTypeProvider
    {
        #region Implementation of INodeTypeProvider

        public bool IsRegistered(MethodInfo method)
        {
            return TryGetNodeTypeProviderAttribute(method) != null;
        }

        public System.Type GetNodeType(MethodInfo method)
        {
            var attribute = TryGetNodeTypeProviderAttribute(method);

            if (attribute == null)
                throw new InvalidOperationException("NodeTypeProviderAttribute not found.");

            return attribute.NodeType;
        }

        #endregion

        private NodeTypeProviderAttribute TryGetNodeTypeProviderAttribute(MethodInfo method)
        {
            var attributes = method.GetCustomAttributes(typeof(NodeTypeProviderAttribute), false);
            return attributes.FirstOrDefault() as NodeTypeProviderAttribute;
        }
    }
}