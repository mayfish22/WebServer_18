using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebServer.Services
{
    //https://blog.miniasp.com/post/2019/12/16/How-to-use-JWT-token-based-auth-in-aspnet-core-31
    public class JWTService
    {
        private readonly int _timeout;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _signKey;
        public JWTService(IConfiguration configuration)
        {
            _timeout = configuration.GetValue<int>("JwtSettings:TokenTimeout");
            _issuer = configuration.GetValue<string>("JwtSettings:Issuer");
            _audience = configuration.GetValue<string>("JwtSettings:Audience");
            _signKey = configuration.GetValue<string>("JwtSettings:SignKey");
        }

        public string GenerateToken(string userName, int? timeout = null)
        {
            timeout ??= _timeout;
            // 設定要加入到 JWT Token 中的聲明資訊(Claims)
            var claims = new List<Claim>();

            // 在 RFC 7519 規格中(Section#4)，總共定義了 7 個預設的 Claims，我們應該只用的到兩種！
            //claims.Add(new Claim(JwtRegisteredClaimNames.Iss, issuer));
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userName)); // User.Identity.Name
            //claims.Add(new Claim(JwtRegisteredClaimNames.Aud, "The Audience"));
            //claims.Add(new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddSeconds(timeout).ToUnixTimeSeconds().ToString()));
            //claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())); // 必須為數字
            //claims.Add(new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())); // 必須為數字
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())); // JWT ID

            var userClaimsIdentity = new ClaimsIdentity(claims);

            // 建立一組對稱式加密的金鑰，主要用於 JWT 簽章之用
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signKey));

            // HmacSha256 有要求必須要大於 128 bits，所以 key 不能太短，至少要 16 字元以上
            // https://stackoverflow.com/questions/47279947/idx10603-the-algorithm-hs256-requires-the-securitykey-keysize-to-be-greater
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            // 建立 SecurityTokenDescriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _issuer,
                Audience = _audience,
                NotBefore = DateTime.Now, // 預設值就是 currentDT
                IssuedAt = DateTime.Now, // 預設值就是 currentDT
                Subject = userClaimsIdentity,
                Expires = DateTime.Now.AddSeconds(timeout.Value),
                SigningCredentials = signingCredentials
            };

            // 產出所需要的 JWT securityToken 物件，並取得序列化後的 Token 結果(字串格式)
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var serializeToken = tokenHandler.WriteToken(securityToken);

            return serializeToken;
        }
    }
}