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
            if (this.controls == null) this.controls = new List<IControl>();
            if (this.fields == null) this.fields = new List<IField>();
            if (this.controlAttr == null) this.controlAttr = new PropertyCollection();
            if (this.viewAttr == null) this.viewAttr = new PropertyCollection();
            if (this.PKColNames == null) this.PKColNames = new List<string>();
        }

        public void AddChildren(List<IPanel> children)
        {
            foreach (IPanel p in children)
            {
                if(this.children.Any(p2 => p2.panelId == p.panelId))
                    throw new Exception("Panel already contains a child with this id.");
                this.children.Add(p);
            }
        }


        public void SetCreationId(int id)
        {
            if (panelId == 0) panelId = id;
            else
            throw new Exception("Panel id already initialized");
        }

        public void SetParentPanel(IPanel parentPanel)
        {
            if (parent == null) parent = parentPanel;
            else
                throw new Exception("Panel parent already initialized");
        }

        public void AddFields(List<IField> fields)
        {
            foreach (IField newField in fields) {
                if (this.fields.Any(f => f.fieldId == newField.fieldId))
                    throw new Exception("Panel already contains a field with this id.");
                this.fields.Add(newField);
            }
        }

        public void AddControls(List<IControl> controls)
        {
            foreach (IControl newControl in controls)
            {
                var dupl = (from c in controls where c.action == newControl.action && c != newControl select c).Count();
                if (Convert.ToInt32(dupl) > 0)
                    throw new Exception("Panel already contains a control for this action.");
                this.controls.Add(newControl);
            }
        }

        public void AddViewAttr(object key, object value)
        {
            if (viewAttr.ContainsKey(key)){
                if(value != viewAttr[key])
                    throw new Exception("Panel view property already set to a different value");
            }
            else viewAttr.Add(key, value);
        }

        public void AddControlAttr(object key, object value)
        {
            if (controlAttr.ContainsKey(key))
            {
                if (value != controlAttr[key])
                    throw new Exception("Panel control property already set to a different value");
            }
            else controlAttr.Add(key, value);
        }
    }
}
