﻿namespace MassTransit.Configuration
{
    using Pipeline;
    using Pipeline.Pipes;
    using PublishPipeSpecifications;
    using Topology;
    using Topology.Observers;


    public class PublishPipeConfiguration :
        IPublishPipeConfiguration
    {
        readonly PublishPipeSpecification _specification;

        public PublishPipeConfiguration(IPublishTopology publishTopology)
        {
            _specification = new PublishPipeSpecification();
            _specification.ConnectPublishPipeSpecificationObserver(new TopologyPublishPipeSpecificationObserver(publishTopology));
        }

        public PublishPipeConfiguration(IPublishPipeSpecification parentSpecification)
        {
            _specification = new PublishPipeSpecification();
            _specification.ConnectPublishPipeSpecificationObserver(new ParentPublishPipeSpecificationObserver(parentSpecification));
        }

        public IPublishPipeSpecification Specification => _specification;
        public IPublishPipeConfigurator Configurator => _specification;

        public IPublishPipe CreatePipe()
        {
            return new PublishPipe(_specification);
        }
    }
}
