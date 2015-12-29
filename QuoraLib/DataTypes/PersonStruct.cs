namespace QuoraLib.DataTypes
{
    public class PersonStruct
    {
        public PersonStruct()
        {
            Proxy = new ProxyStruct();
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Mail { get; set; }
        public string Pass { get; set; }
        public ProxyStruct Proxy { get; set; }
        public string Result { get; set; }
    }
}