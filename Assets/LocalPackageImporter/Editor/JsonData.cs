using System;
using UnityEngine;

namespace LocalPackageImporter
{
    [Serializable]
    public class JsonData
    {
        public Category category;
        public string description;
        public string id;
        public Link link;
        public string pubdate;
        public Publisher publisher;
        public string publishnotes;
        public string title;
        public string unity_version;
        public string upload_id;
        public string version;
        public string version_id;
    }

    [Serializable]
    public class Category
    {
        public string id;
        public string label;
    }

    [Serializable]
    public class Link
    {
        public string id;
        public string type;
    }

    [Serializable]
    public class Publisher
    {
        public string id;
        public string label;
    }
}