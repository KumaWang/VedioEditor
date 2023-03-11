namespace VedioEditor
{
    public struct Additional
    {
        public Additional(string template, double x, double y, double fontSize)
        {
            Template = template;
            X = x;
            Y = y;
            FontSize = fontSize;
        }

        public double FontSize { get; }

        public string Template { get; }

        public double X { get; }

        public double Y { get; }
    }
}
