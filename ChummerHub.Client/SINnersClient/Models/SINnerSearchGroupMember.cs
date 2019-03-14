﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace SINners.Models
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Microsoft.Rest;
    using Microsoft.Rest.Serialization;

    public partial class SINnerSearchGroupMember
    {
        /// <summary>
        /// Initializes a new instance of the SINnerSearchGroupMember class.
        /// </summary>
        public SINnerSearchGroupMember() { }

        /// <summary>
        /// Initializes a new instance of the SINnerSearchGroupMember class.
        /// </summary>
        public SINnerSearchGroupMember(SINner mySINner = default(SINner), string username = default(string))
        {
            MySINner = mySINner;
            Username = username;
        }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "mySINner")]
        public SINner MySINner { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; }

        /// <summary>
        /// Validate the object. Throws ValidationException if validation fails.
        /// </summary>
        public virtual void Validate()
        {
            if (this.MySINner != null)
            {
                this.MySINner.Validate();
            }
        }
    }
}
