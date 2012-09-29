using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using _min.Interfaces;
using System.Data;

namespace _min.Models
{
    class Panel : IPanel
    {
        public string tableName { get; private set; }
        public PropertyCollection viewAttr {get; private set;}
        public PropertyCollection controlAttr { get; private set; }
        public List<IPanel> children { get; private set; }
        public List<IField> fields { get; private set; }    // vratane dockov
        public List<IControl> controls { get; private set; }
        public List<string> PKColNames { get; private set; }
        public DataRow PK { get; private set; }
        public IPanel parent { get; private set; }
        public int panelId { get; set; }
        public int typeId { get; private set; }
        public string typeName { get; private set; }
        // holder (if != null) in attr["holder"]

        public Panel(string tableName, int panelId, int typeId, string typeName, List<IPanel> children,
            List<IField> fields, List<IControl> controls, List<string> PKColNames, DataRow PK = null,  
            PropertyCollection viewAttr = null, PropertyCollection controlAttr = null, IPanel parent = null){
            this.tableName = tableName;
            this.viewAttr = viewAttr == null ? new PropertyCollection() : viewAttr;
            this.controlAttr = controlAttr==null?new PropertyCollection():controlAttr;
            this.panelId = panelId;
            this.children = children;
            this.fields = fields;
            this.controls = controls;
            this.PKColNames = PKColNames;
            this.PK = PK;
            this.typeId = typeId;
            this.typeName = typeName;
            this.parent = parent;
        }

        public void AddChildren(List<IPanel> children)
        {
            foreach (IPanel p in children) this.children.Add(p);
        }


        public void SetCreationId(int id)
        {
            if (panelId == 0) panelId = id;
            throw new Exception("Panel id already initialized");
        }
    }
}
