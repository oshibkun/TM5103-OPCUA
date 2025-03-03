using LibUA;
using LibUA.Core;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace TM5103.OPCUA
{
    class UAServer
    {
        public class DemoApplication : LibUA.Server.Application
        {
            private readonly ApplicationDescription uaAppDesc;
            private readonly NodeObject ItemsRoot;
            private readonly NodeVariable[] TrendNodes;
            private X509Certificate2 appCertificate = null;
            private RSA cryptPrivateKey = null;

            public override X509Certificate2 ApplicationCertificate
            {
                get { return appCertificate; }
            }

            public override RSA ApplicationPrivateKey
            {
                get { return cryptPrivateKey; }
            }

            public DemoApplication()
            {
                LoadCertificateAndPrivateKey();

                uaAppDesc = new ApplicationDescription(
                    "urn:TM5103.OPCUA", "https://yandex.ru/search/?text=TM5103",
                    new LocalizedText("en-US", "TM5103 OPCUA Server"), ApplicationType.Server,
                    null, null, null);

                // ItemsRoot = new NodeObject(new NodeId(2, 0), new QualifiedName("COM"), new LocalizedText("COM"),
                //     new LocalizedText("COM"), 0, 0, 0);
                ushort i = 2;
                foreach (var port in Settings.AllSettings) //обходим все порты
                {

                    NodeObject ItemsRoot2 = new NodeObject(new NodeId(i, 0), new QualifiedName((string)port[0]), new LocalizedText((string)port[0]),
                    new LocalizedText((string)port[0]), 0, 0, 0);
                    Console.WriteLine("ua " + i + " add port " + (string)port[0]);
                    AddressSpaceTable[new NodeId(UAConst.ObjectsFolder)].References.Add(new ReferenceNode(new NodeId(UAConst.Organizes), new NodeId(i, 0), false));
                    ItemsRoot2.References.Add(new ReferenceNode(new NodeId(UAConst.Organizes),
                    new NodeId(UAConst.ObjectsFolder), true));
                    AddressSpaceTable.TryAdd(ItemsRoot2.Id, ItemsRoot2);
                    ushort j = 1;
                    foreach (var addr in (Dictionary<int, Dictionary<int, bool>>)port[2])
                    {

                        NodeObject AddrRoot = new NodeObject(new NodeId(i, $"Adr{j}"), new QualifiedName(addr.Key.ToString()), new LocalizedText(addr.Key.ToString()),
                            new LocalizedText(addr.Key.ToString()), 0, 0, 0);

                        AddressSpaceTable[new NodeId(i, 0)].References
                            .Add(new ReferenceNode(new NodeId(UAConst.Organizes), new NodeId(i, $"Adr{j}"), false));

                        AddrRoot.References.Add(new ReferenceNode(new NodeId(UAConst.Organizes),
                                new NodeId(i, 0), true));

                        AddressSpaceTable.TryAdd(AddrRoot.Id, AddrRoot);
                        foreach (var chan in addr.Value)
                        {
                            NodeVariable channode;
                            var nodeTypeFloat2 = new NodeId(0, 10);
                            var id = string.Format("Channel {0}", chan.Key.ToString());
                            channode = new NodeVariable(new NodeId(i, $"Addr{addr.Key}Chan{chan.Key}"), new QualifiedName(id),
                                new LocalizedText(id), new LocalizedText(id), 0, 0,
                                AccessLevel.CurrentRead,
                                AccessLevel.CurrentRead, 0, false, nodeTypeFloat2);

                            AddrRoot.References.Add(new ReferenceNode(new NodeId(UAConst.Organizes), channode.Id, false));
                            channode.References.Add(new ReferenceNode(new NodeId(UAConst.Organizes), AddrRoot.Id, true));
                            AddressSpaceTable.TryAdd(channode.Id, channode);
                        }
                        j++;
                    }
                    i++;
                }

                /*

                    ItemsRoot2 = new NodeObject(new NodeId(3, 0), new QualifiedName("COM2"), new LocalizedText("COM2"),
                    new LocalizedText("COM2"), 0, 0, 0);
                // Objects organizes Items
                AddressSpaceTable[new NodeId(UAConst.ObjectsFolder)].References
                    .Add(new ReferenceNode(new NodeId(UAConst.Organizes), new NodeId(2, 0), false));
                AddressSpaceTable[new NodeId(UAConst.ObjectsFolder)].References
                    .Add(new ReferenceNode(new NodeId(UAConst.Organizes), new NodeId(3, 0), false));
                ItemsRoot.References.Add(new ReferenceNode(new NodeId(UAConst.Organizes),
                    new NodeId(UAConst.ObjectsFolder), true));
                ItemsRoot2.References.Add(new ReferenceNode(new NodeId(UAConst.Organizes),
                    new NodeId(UAConst.ObjectsFolder), true));
                AddressSpaceTable.TryAdd(ItemsRoot.Id, ItemsRoot);
                AddressSpaceTable.TryAdd(ItemsRoot2.Id, ItemsRoot2);

                TrendNodes = new NodeVariable[8];
                var nodeTypeFloat = new NodeId(0, 10);
                for (int i = 0; i < TrendNodes.Length; i++)
                {
                    var id = string.Format("Channel {0}", (1 + i).ToString());
                    TrendNodes[i] = new NodeVariable(new NodeId(2, (uint)(1 + i)), new QualifiedName(id),
                        new LocalizedText(id), new LocalizedText(id), 0, 0,
                        AccessLevel.CurrentRead,
                        AccessLevel.CurrentRead, 0, false, nodeTypeFloat);

                    ItemsRoot.References.Add(new ReferenceNode(new NodeId(UAConst.Organizes), TrendNodes[i].Id, false));
                    TrendNodes[i].References.Add(new ReferenceNode(new NodeId(UAConst.Organizes), ItemsRoot.Id, true));
                    AddressSpaceTable.TryAdd(TrendNodes[i].Id, TrendNodes[i]);
                
                }
                */

            }

            public override bool SessionValidateClientUser(object session, object userIdentityToken)
            {
                if (userIdentityToken is UserIdentityAnonymousToken)
                {
                    return true;
                }
                else if (userIdentityToken is UserIdentityUsernameToken)
                {
                    _ = (userIdentityToken as UserIdentityUsernameToken).Username;
                    _ =
                        (new UTF8Encoding()).GetString((userIdentityToken as UserIdentityUsernameToken).PasswordHash);

                    return true;
                }

                throw new Exception("Unhandled user identity token type");
            }

            private ApplicationDescription CreateApplicationDescriptionFromEndpointHint(string endpointUrlHint)
            {
                string[] discoveryUrls = uaAppDesc.DiscoveryUrls;
                if (discoveryUrls == null && !string.IsNullOrEmpty(endpointUrlHint))
                {
                    discoveryUrls = new string[] { endpointUrlHint };
                }

                return new ApplicationDescription(uaAppDesc.ApplicationUri, uaAppDesc.ProductUri, uaAppDesc.ApplicationName,
                    uaAppDesc.Type, uaAppDesc.GatewayServerUri, uaAppDesc.DiscoveryProfileUri, discoveryUrls);
            }

            public override IList<EndpointDescription> GetEndpointDescriptions(string endpointUrlHint)
            {
                var certStr = ApplicationCertificate.Export(X509ContentType.Cert);
                ApplicationDescription localAppDesc = CreateApplicationDescriptionFromEndpointHint(endpointUrlHint);

                var epNoSecurity = new EndpointDescription(
                    endpointUrlHint, localAppDesc, certStr,
                    MessageSecurityMode.None, Types.SLSecurityPolicyUris[(int)SecurityPolicy.None],
                    new UserTokenPolicy[]
                    {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.None]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256Sha256]),
                    }, Types.TransportProfileBinary, 0);

                var epSignBasic128Rsa15 = new EndpointDescription(
                    endpointUrlHint, localAppDesc, certStr,
                    MessageSecurityMode.Sign, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic128Rsa15],
                    new UserTokenPolicy[]
                    {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.None]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256Sha256]),
                    }, Types.TransportProfileBinary, 0);

                var epSignBasic256 = new EndpointDescription(
                    endpointUrlHint, localAppDesc, certStr,
                    MessageSecurityMode.Sign, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256],
                    new UserTokenPolicy[]
                    {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.None]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256Sha256]),
                    }, Types.TransportProfileBinary, 0);

                var epSignBasic256Sha256 = new EndpointDescription(
                    endpointUrlHint, localAppDesc, certStr,
                    MessageSecurityMode.Sign, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256Sha256],
                    new UserTokenPolicy[]
                    {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.None]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256Sha256]),
                    }, Types.TransportProfileBinary, 0);

                var epSignRsa128Sha256 = new EndpointDescription(
                    endpointUrlHint, localAppDesc, certStr,
                    MessageSecurityMode.Sign, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Aes128_Sha256_RsaOaep],
                    new UserTokenPolicy[]
                    {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.None]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Aes128_Sha256_RsaOaep]),
                    }, Types.TransportProfileBinary, 0);

                var epSignRsa256Sha256 = new EndpointDescription(
                    endpointUrlHint, localAppDesc, certStr,
                    MessageSecurityMode.Sign, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Aes256_Sha256_RsaPss],
                    new UserTokenPolicy[]
                    {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.None]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Aes256_Sha256_RsaPss]),
                    }, Types.TransportProfileBinary, 0);

                var epSignEncryptBasic128Rsa15 = new EndpointDescription(
                    endpointUrlHint, localAppDesc, certStr,
                    MessageSecurityMode.SignAndEncrypt, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic128Rsa15],
                    new UserTokenPolicy[]
                    {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.None]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256Sha256]),
                    }, Types.TransportProfileBinary, 0);

                var epSignEncryptBasic256 = new EndpointDescription(
                    endpointUrlHint, localAppDesc, certStr,
                    MessageSecurityMode.SignAndEncrypt, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256],
                    new UserTokenPolicy[]
                    {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.None]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256Sha256]),
                    }, Types.TransportProfileBinary, 0);

                var epSignEncryptBasic256Sha256 = new EndpointDescription(
                    endpointUrlHint, localAppDesc, certStr,
                    MessageSecurityMode.SignAndEncrypt, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256Sha256],
                    new UserTokenPolicy[]
                    {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.None]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256Sha256]),
                    }, Types.TransportProfileBinary, 0);

                var epSignEncryptRsa128Sha256 = new EndpointDescription(
                    endpointUrlHint, localAppDesc, certStr,
                    MessageSecurityMode.SignAndEncrypt, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Aes128_Sha256_RsaOaep],
                    new UserTokenPolicy[]
                    {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.None]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Aes128_Sha256_RsaOaep]),
                    }, Types.TransportProfileBinary, 0);

                var epSignEncryptRsa256Sha256 = new EndpointDescription(
                    endpointUrlHint, localAppDesc, certStr,
                    MessageSecurityMode.SignAndEncrypt, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Aes256_Sha256_RsaPss],
                    new UserTokenPolicy[]
                    {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.None]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Aes256_Sha256_RsaPss]),
                    }, Types.TransportProfileBinary, 0);

                return new EndpointDescription[]
                {
                    epNoSecurity,
                    epSignRsa256Sha256, epSignEncryptRsa256Sha256,
                    epSignRsa128Sha256, epSignEncryptRsa128Sha256,
                    epSignBasic256Sha256, epSignEncryptBasic256Sha256,
                    epSignBasic256, epSignEncryptBasic256,
                    epSignBasic128Rsa15, epSignEncryptBasic128Rsa15
                };
            }

            public override ApplicationDescription GetApplicationDescription(string endpointUrlHint)
            {
                return CreateApplicationDescriptionFromEndpointHint(endpointUrlHint);
            }




            public void DataUpdate(ushort ns, int addr, int chan, float val, StatusCode status)
            {
                NodeId node = new NodeId(ns, $"Addr{addr}Chan{chan}");
                var dv = new DataValue(val, status, DateTime.Now);
                MonitorNotifyDataChange(node, dv);
            }
            public bool PlayRow(int chan, float val)
            {

                NodeId node = new NodeId(2, (uint)chan);
                var dv = new DataValue(val, StatusCode.Good, DateTime.Now);
                MonitorNotifyDataChange(node, dv);
                return true;
            }

            private void LoadCertificateAndPrivateKey()
            {
                try
                {
                    // Try to load existing (public key) and associated private key
                    appCertificate = new X509Certificate2("ServerCert.der");
                    cryptPrivateKey = RSA.Create();
                    cryptPrivateKey.KeySize = 2048;

                    var rsaPrivParams = UASecurity.ImportRSAPrivateKey(File.ReadAllText("ServerKey.pem"));
                    cryptPrivateKey.ImportParameters(rsaPrivParams);
                }
                catch
                {
                    // Make a new certificate (public key) and associated private key
                    var dn = new X500DistinguishedName("CN=Server certificate;OU=Demo organization",
                        X500DistinguishedNameFlags.UseSemicolons);
                    SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                    sanBuilder.AddUri(new Uri("urn:DemoApplication"));

                    using (RSA rsa = RSA.Create(4096))
                    {
                        var request = new CertificateRequest(dn, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                        request.CertificateExtensions.Add(sanBuilder.Build());
                        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

                        request.CertificateExtensions.Add(new X509KeyUsageExtension(
                            X509KeyUsageFlags.DigitalSignature |
                            X509KeyUsageFlags.NonRepudiation |
                            X509KeyUsageFlags.DataEncipherment |
                            X509KeyUsageFlags.KeyEncipherment, false));

                        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection {
                            new Oid("1.3.6.1.5.5.7.3.8"),
                            new Oid("1.3.6.1.5.5.7.3.1"),
                            new Oid("1.3.6.1.5.5.7.3.2"),
                            new Oid("1.3.6.1.5.5.7.3.3"),
                            new Oid("1.3.6.1.5.5.7.3.4"),
                            new Oid("1.3.6.1.5.5.7.3.8"),
                            new Oid("1.3.6.1.5.5.7.3.9"),
                        }, true));

                        var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)),
                            new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));

                        appCertificate = new X509Certificate2(certificate.Export(X509ContentType.Pfx, ""),
                            "", X509KeyStorageFlags.DefaultKeySet);

                        var certPrivateParams = rsa.ExportParameters(true);
                        File.WriteAllText("ServerCert.der", UASecurity.ExportPEM(appCertificate));
                        File.WriteAllText("ServerKey.pem", UASecurity.ExportRSAPrivateKey(certPrivateParams));

                        cryptPrivateKey = RSA.Create();
                        cryptPrivateKey.KeySize = 2048;
                        cryptPrivateKey.ImportParameters(certPrivateParams);
                    }
                }
            }
        }

    }
}
