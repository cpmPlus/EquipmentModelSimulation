using System;

namespace EquipmentModelSimulation
{
    public class Equipment
    {

        public string Name { get; set; }

        public Equipment(string name)
        {
            Name = name;
        }

        public void Act()
        {
            throw new NotImplementedException("Implement me");
        }
    }
}
