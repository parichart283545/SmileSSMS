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
    
    public partial class Transaction
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Transaction()
        {
            this.TransactionDetails = new HashSet<TransactionDetail>();
        }
    
        public int TransactionID { get; set; }
        public Nullable<int> SMSID { get; set; }
        public Nullable<int> TransactionStatusID { get; set; }
        public string ReferenceID { get; set; }
        public Nullable<int> SumPhone { get; set; }
        public Nullable<int> UsedCredit { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<bool> GetDeliveryNotify { get; set; }
        public Nullable<int> TransactionHeaderId { get; set; }
        public Nullable<System.DateTime> SendingStatusUpdateDate { get; set; }
        public string Success { get; set; }
        public string Fail { get; set; }
        public string Block { get; set; }
        public string Expired { get; set; }
        public string Processing { get; set; }
        public string Unknown { get; set; }
        public string XMLResult { get; set; }
        public Nullable<int> Credit { get; set; }
        public string StatusMessage { get; set; }
    
        public virtual SM SM { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TransactionDetail> TransactionDetails { get; set; }
        public virtual TransactionStatu TransactionStatu { get; set; }
    }
}
