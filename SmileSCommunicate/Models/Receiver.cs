//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SmileSCommunicate.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Receiver
    {
        public int ReceiverID { get; set; }
        public Nullable<int> SMSID { get; set; }
        public string PhoneNo { get; set; }
    
        public virtual SMS SMS { get; set; }
    }
}
