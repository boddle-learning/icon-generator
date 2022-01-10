using System;
using System.Collections.Generic;
using UnityEngine;

namespace Boddle.IconGenerator
{
    [Serializable]
    public class IconGenerationConfig
    {
        public GameObject gameObject;
        public List<MaterialPropertyBlockProperty> properties = new List<MaterialPropertyBlockProperty>();
    }

    [Serializable]
    public class MaterialPropertyBlockProperty
    {
        [SerializeField]
        private string name;

        [SerializeField, HideInInspector]
        private PropertyType type;

        [SerializeField, HideInInspector]
        private string stringValue;

        [SerializeField, HideInInspector]
        private int intValue;

        [SerializeField, HideInInspector]
        private float floatValue;

        [SerializeField, HideInInspector]
        private Color colorValue;

        public string Name => name;

        public bool HasValue { get; private set; }

        public object Value
        {
            get
            {
                if (type == PropertyType.String)
                {
                    return stringValue;
                }
                else if (type == PropertyType.Int)
                {
                    return intValue;
                }
                else if (type == PropertyType.Float)
                {
                    return floatValue;
                }
                else if (type == PropertyType.Color)
                {
                    return colorValue;
                }

                return null;
            }
            set
            {
                HasValue = false;
                if (value is null)
                {
                    Debug.LogError("Value provided is null.");
                }
                else
                {
                    HasValue = true;
                    if (value is int intValue)
                    {
                        this.intValue = intValue;
                        type = PropertyType.Int;
                    }
                    else if (value is float floatValue)
                    {
                        this.floatValue = floatValue;
                        type = PropertyType.Float;
                    }
                    else if (value is string stringValue)
                    {
                        this.stringValue = stringValue;
                        type = PropertyType.String;
                    }
                    else if (value is Color colorValue)
                    {
                        this.colorValue = colorValue;
                        type = PropertyType.Color;
                    }
                    else
                    {
                        Debug.LogError($"Property of type {value.GetType()} is not supported.");
                        HasValue = false;
                    }
                }
            }
        }

        public MaterialPropertyBlockProperty(string name, string value)
        {
            this.name = name;
            stringValue = value;
            type = PropertyType.String;
            HasValue = true;
        }

        public MaterialPropertyBlockProperty(string name, float value)
        {
            this.name = name;
            floatValue = value;
            type = PropertyType.Float;
            HasValue = true;
        }

        public MaterialPropertyBlockProperty(string name, int value)
        {
            this.name = name;
            intValue = value;
            type = PropertyType.Int;
            HasValue = true;
        }

        public MaterialPropertyBlockProperty(string name, Color value)
        {
            this.name = name;
            colorValue = value;
            type = PropertyType.Color;
            HasValue = true;
        }

        public MaterialPropertyBlockProperty(string name, bool value) : this(name, value ? 1 : 0)
        {

        }

        public MaterialPropertyBlockProperty(string name, object value)
        {
            this.name = name;
            Value = value;
        }
    }
}