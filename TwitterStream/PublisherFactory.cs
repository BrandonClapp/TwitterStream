﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TwitterStream.Config;
using TwitterStream.Config.Objects;

namespace TwitterStream
{
    public static class PublisherFactory
    {

        private static IDictionary<string, ITweetPublisher> _loadedPublishers;

        public static IDictionary<string, ITweetPublisher> GetRegistered()
        {
            if (_loadedPublishers != null)
                return _loadedPublishers;

            var settings = ConfigManager.LoadConfig<PublisherRegistry>("publishers");
            var publishers = new Dictionary<string, PublisherConfig>();

            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var isPublisher = typeof(ITweetPublisher).IsAssignableFrom(type) 
                    && typeof(ITweetPublisher).Name != type.Name;

                if (isPublisher)
                {
                    PublisherConfig pubSettings = settings.Publishers.FirstOrDefault(p => p.Name == type.Name);
                    publishers.Add(type.Name, pubSettings);
                }
            }

            var loadedPublishers = new Dictionary<string, ITweetPublisher>();
            foreach (var publisher in publishers)
            {
                loadedPublishers.Add(publisher.Key, Create(publisher.Key, publisher.Value));
            }

            _loadedPublishers = loadedPublishers;
            return loadedPublishers;
        }

        private static ITweetPublisher Create(string publisherName, PublisherConfig config)
        {
            var pubType = Assembly.GetExecutingAssembly()
                .GetExportedTypes()
                .First(t => t.FullName.EndsWith(publisherName));

            var instance = (ITweetPublisher)Activator.CreateInstance(pubType);
            //_loadedPublishers.TryGetValue(publisherName, out var pubSettings);
            instance.Init(config);
            return instance;
        }
    }
}