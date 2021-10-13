using System;
using System.Collections.Generic;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Xml;

namespace PortailsOpacBase
{
    public class SamlSecurityTokenHandlerEx : SamlSecurityTokenHandler, ISecurityTokenValidator
    {
        public override bool CanReadToken(string securityToken)
        {
            return base.CanReadToken(XmlReader.Create(new StringReader(securityToken)));
        }

        public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters,
            out SecurityToken validatedToken)
        {
            validatedToken = ReadToken(new XmlTextReader(new StringReader(securityToken)), Configuration.ServiceTokenResolver);
            return new ClaimsPrincipal(ValidateToken(validatedToken)); ;
        }

        public int MaximumTokenSizeInBytes { get; set; }
    }

    public class EncryptedSecurityTokenHandlerEx : EncryptedSecurityTokenHandler, ISecurityTokenValidator
    {
        public EncryptedSecurityTokenHandlerEx(SecurityTokenResolver securityTokenResolver)
        {
            Configuration = new SecurityTokenHandlerConfiguration
            {
                ServiceTokenResolver = securityTokenResolver
            };
        }

        public override bool CanReadToken(string securityToken)
        {
            return base.CanReadToken(new XmlTextReader(new StringReader(securityToken)));
        }

        public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
        {
            validatedToken = ReadToken(new XmlTextReader(new StringReader(securityToken)), Configuration.ServiceTokenResolver);
            if (ContainingCollection != null)
            {
                return new ClaimsPrincipal(ContainingCollection.ValidateToken(validatedToken));
            }
            return new ClaimsPrincipal(base.ValidateToken(validatedToken));
        }

        public int MaximumTokenSizeInBytes { get; set; }
    }
}