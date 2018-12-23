using System;

namespace TFlexOmpFix
{
    public class OwnerNotFoundException : Exception
    {
        public string Owner { get; set; }

        public OwnerNotFoundException() : base()
        {
        }

        public OwnerNotFoundException(string owner) : this()
        {
            Owner = owner;
        }

        public override string Message
        {
            get
            {
                return string.Format("Владелец {0} не найден", Owner);
            }
        }
    }
}