//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SmileSCommunicateRESTfulService.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class SMSQueueHeader
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SMSQueueHeader()
        {
            this.SMSQueueDetails = new HashSet<SMSQueueDetail>();
        }
    
        public int SMSQueueHeaderId { get; set; }
        public Nullable<int> ProjectId { get; set; }
        public Nullable<int> TotalSMS { get; set; }
        public string Remark { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> SendDate { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<SMSQueueDetail> SMSQueueDetails { get; set; }
    }
}
