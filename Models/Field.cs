using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using _min.Interfaces;
using _min.Models;
using System.Data;

namespace _min.Models
{
    class Field : IField
    {
        public int fieldId { get; private set; }
        public string column {get; private set;}
        public string typeName {get; private set;}
        public int typeId { get; private set; }
        public string errMsg { get; private set; }
        public int panelId { get; set; }        // TODO add safety
        public PropertyCollection attr { get; private set; }
        public PropertyCollection rules {get; private set; }
        public virtual object value { get; set; }

        public Field(int fieldId, string column, int typeId, string typeName, int panelId, 
            PropertyCollection attr = null, PropertyCollection rules = null)
        {
            this.fieldId = fieldId;
            this.column = column;
            this.typeName = typeName;
            this.panelId = panelId;
            this.typeId = typeId;
            this.attr = attr==null?new PropertyCollection():attr;
            this.rules = rules==null?new PropertyCollection():rules;
            value = null;
            errMsg = "";
            
        }

        //todo validation
        public virtual bool Validate(object value) { 
            return true;
        }


        public virtual bool ValidateSelf()
        {
            return true;
            
        }


        public void SetCreationId(int id)
        {
            if (fieldId == 0) fieldId = id;
            else
            throw new Exception("Field id already initialized");
        }
    }

    class FKField : Field, IFKField
    {
        private object _value;
        public override object value 
        {
            get {
                return _value;
            }
            set {
                if (!(value is string) && !(value == null))
                    throw new ArgumentException("Value of a mapping field must be string or null");
                else
                    this._value = value;   
            } 
        }
        public IFK fk { get; private set; }

        public FKField(int fieldId, string column, int typeId, string typeName, int panelId, IFK fk,
            PropertyCollection attr = null, PropertyCollection rules = null)
            : base(fieldId, column, typeId, typeName, panelId, attr, rules) 
        {
            this.fk = fk;
        }

        public override bool ValidateSelf()
        {
            return fk.validateInput((string)value);
        }

    }

    class M2NMappingField : Field, IM2NMappingField
    {
        private object _value;
        public override object value
        {
            get {
                return _value;
            }
            set {
                if (!(value is List<string>) && !(value == null))
                    throw new ArgumentException("Value of a mapping field must be List<string> or null");
                else
                    this._value = value;
            }
        }
        public IM2NMapping mapping { get; private set; }


        public M2NMappingField(int fieldId, string column, int typeId, string typeName, int panelId, IM2NMapping mapping,
            PropertyCollection attr = null, PropertyCollection rules = null)
            : base(fieldId, column, typeId, typeName, panelId, attr, rules)
        {
            this.mapping = mapping;
        }

        public override bool ValidateSelf()
        {
            return mapping.validateWholeInput((List<string>)value);

        }
    }
        
}