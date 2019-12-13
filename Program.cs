using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace vcf21tovcf30
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] vcf21 = File.ReadAllLines(args[0]);

            string[] vcf30 = vcf21
                .Select(line => ConvertLineFrom21to30(line))
                .ToArray();

            File.WriteAllLines(args[1], vcf30);
        }

        private static string ConvertLineFrom21to30(string line)
        {
            if (line == "Version:2.1")
                return "Version:3.0";

            if (line.StartsWith("PHOTO"))
                return line
                    .Replace(";ENCODING=BASE64", ";ENCODING=b")
                    .Replace(";JPEG", ";TYPE=JPEG");

            if (line.Contains(";ENCODING=QUOTED-PRINTABLE"))
            {
                int posColon = line.IndexOf(':');
                string header = line.Substring(0, posColon)
                    .Replace(";ENCODING=QUOTED-PRINTABLE", string.Empty);
                string charsetAttribute = header.Split(';')
                    .SingleOrDefault(attribute => attribute.StartsWith("CHARSET="));
                string charset = "UTF-8";
                if (!string.IsNullOrWhiteSpace(charsetAttribute))
                    charset = charsetAttribute.Substring("CHARSET=".Length);

                string unencodedValue = Decode(line.Substring(posColon + 1), charset);
                string value = unencodedValue
                    .Replace("\r\n", @"\n")
                    .Replace("\n", @"\n")
                    .Replace("\r", string.Empty);

                return header + ":" + value;
            }

            return line;
        }

        /// <summary>
        /// Decodes quoted printable text. From https://stackoverflow.com/a/36803911
        /// </summary>
        private static string DecodeQuotedPrintable(string data, string encoding)
        {
            data = data.Replace("=\r\n", "");
            for (int position = -1; (position = data.IndexOf("=", position + 1)) != -1;)
            {
                string leftpart = data.Substring(0, position);
                System.Collections.ArrayList hex = new System.Collections.ArrayList();
                hex.Add(data.Substring(1 + position, 2));
                while (position + 3 < data.Length && data.Substring(position + 3, 1) == "=")
                {
                    position = position + 3;
                    hex.Add(data.Substring(1 + position, 2));
                }
                byte[] bytes = new byte[hex.Count];
                for (int i = 0; i < hex.Count; i++)
                {
                    bytes[i] = System.Convert.ToByte(new string(((string)hex[i]).ToCharArray()), 16);
                }
                string equivalent = System.Text.Encoding.GetEncoding(encoding).GetString(bytes);
                string rightpart = data.Substring(position + 3);
                data = leftpart + equivalent + rightpart;
            }
            return data;
        }

        private static string Decode(string input, string bodycharset)
        {
            var i = 0;
            var output = new List<byte>();
            while (i < input.Length)
            {
                if (input[i] == '=' && input[i + 1] == '\r' && input[i + 2] == '\n')
                {
                    //Skip
                    i += 3;
                }
                else if (input[i] == '=')
                {
                    string sHex = input;
                    sHex = sHex.Substring(i + 1, 2);
                    int hex = Convert.ToInt32(sHex, 16);
                    byte b = Convert.ToByte(hex);
                    output.Add(b);
                    i += 3;
                }
                else
                {
                    output.Add((byte)input[i]);
                    i++;
                }
            }


            if (String.IsNullOrEmpty(bodycharset))
                return Encoding.UTF8.GetString(output.ToArray());
            else
            {
                if (String.Compare(bodycharset, "ISO-2022-JP", true) == 0)
                    return Encoding.GetEncoding("Shift_JIS").GetString(output.ToArray());
                else
                    return Encoding.GetEncoding(bodycharset).GetString(output.ToArray());
            }

        }
    }
}