using System;

namespace TFlexOmpFix
{
    public class StateNotFoundException : Exception
    {
        public string BoType { get; set; }

        public string State { get; set; }

        public StateNotFoundException() : base()
        {
        }

        public StateNotFoundException(string botype, string state) : this()
        {
            BoType = botype;
            State = state;
        }

        public override string Message
        {
            get
            {
                return string.Format("У объекта \"{0}\" отсутствует статус \"{1}\"", BoType, State);
            }
        }
    }
}