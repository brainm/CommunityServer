/*
(c) Copyright Ascensio System SIA 2010-2014

This program is a free software product.
You can redistribute it and/or modify it under the terms 
of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of 
any third-party rights.

This program is distributed WITHOUT ANY WARRANTY; without even the implied warranty 
of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see 
the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html

You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.

The  interactive user interfaces in modified source and object code versions of the Program must 
display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
 
Pursuant to Section 7(b) of the License you must retain the original Product logo when 
distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under 
trademark law for use of our trademarks.
 
All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode
*/

using System.Linq;
using System.Net.Mime;
using System.Text;

namespace ASC.Web.Core.Files
{
    public static class ContentDispositionUtil
    {
        public static string GetHeaderValue(string fileName, bool inline = false, bool withoutBase = false)
        {
            // If fileName contains any Unicode characters, encode according
            // to RFC 2231 (with clarifications from RFC 5987)
            if (fileName.Any(c => (int)c > 127))
            {
                return CreateRfc2231HeaderValue(fileName, inline, withoutBase);
            }

            // Knowing there are no Unicode characters in this fileName, rely on
            // ContentDisposition.ToString() to encode properly.
            // In .Net 4.0, ContentDisposition.ToString() throws FormatException if
            // the file name contains Unicode characters.
            // In .Net 4.5, ContentDisposition.ToString() no longer throws FormatException
            // if it contains Unicode, and it will not encode Unicode as we require here.
            // The Unicode test above is identical to the 4.0 FormatException test,
            // allowing this helper to give the same results in 4.0 and 4.5.         
            var disposition = new ContentDisposition { FileName = fileName, Inline = inline };
            return disposition.ToString();
        }

        private const string HexDigits = "0123456789ABCDEF";

        private static void AddByteToStringBuilder(byte b, StringBuilder builder)
        {
            builder.Append('%');

            int i = b;
            AddHexDigitToStringBuilder(i >> 4, builder);
            AddHexDigitToStringBuilder(i%16, builder);
        }

        private static void AddHexDigitToStringBuilder(int digit, StringBuilder builder)
        {
            builder.Append(HexDigits[digit]);
        }

        private static string CreateRfc2231HeaderValue(string filename, bool inline, bool withoutBase)
        {
            var builder = new StringBuilder((inline ? "inline" : "attachment") 
                + (withoutBase ? "" : "; filename=\"" + filename + "\"" )
                + "; filename*=UTF-8''");

            var filenameBytes = Encoding.UTF8.GetBytes(filename);
            foreach (var b in filenameBytes)
            {
                if (IsByteValidHeaderValueCharacter(b))
                {
                    builder.Append((char)b);
                }
                else
                {
                    AddByteToStringBuilder(b, builder);
                }
            }

            return builder.ToString();
        }

        // Application of RFC 2231 Encoding to Hypertext Transfer Protocol (HTTP) Header Fields, sec. 3.2
        // http://greenbytes.de/tech/webdav/draft-reschke-rfc2231-in-http-latest.html
        private static bool IsByteValidHeaderValueCharacter(byte b)
        {
            if ((byte)'0' <= b && b <= (byte)'9')
            {
                return true; // is digit
            }
            if ((byte)'a' <= b && b <= (byte)'z')
            {
                return true; // lowercase letter
            }
            if ((byte)'A' <= b && b <= (byte)'Z')
            {
                return true; // uppercase letter
            }

            switch (b)
            {
                case (byte)'-':
                case (byte)'.':
                case (byte)'_':
                case (byte)'~':
                case (byte)':':
                case (byte)'!':
                case (byte)'$':
                case (byte)'&':
                case (byte)'+':
                    return true;
            }

            return false;
        }
    }
}