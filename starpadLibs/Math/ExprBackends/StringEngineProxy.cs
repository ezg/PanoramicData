using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Linq;

namespace starPadSDK.MathExpr {
    public class StringEngineProxy : Engine {
        private MathML _mml = new MathML(false, false, MathML.Namespace, true);
        public override bool Exists { get { return _records.Count > 0; } }
        private class StringEngineRecord {
            private AppDomain _domain;
            public StringEngine Interface { get; private set; }
            public string EngName { get; private set; }
            private string[] _extradlls;
            private string _typename, _assyname;
            public bool Valid { get; private set; }
            private FileInfo _fi;
            public StringEngineRecord(Type t, FileInfo fi) {
                _fi = fi;
                object[] attrs = t.GetCustomAttributes(typeof(EngineNameAttribute), false);
                EngName = ((EngineNameAttribute)attrs[0]).EngineName;
                attrs = t.GetCustomAttributes(typeof(ExtraDLLAttribute), false);
                _extradlls = attrs.Cast<ExtraDLLAttribute>().Select((eda) => eda.DLLName).ToArray();
                Valid = _extradlls.All((name) => File.Exists(name));
                _typename = t.FullName;
                _assyname = t.Assembly.FullName;
                _domain = null;
                Interface = null;
            }
            public void Activate() {
                AppDomainSetup ads = new AppDomainSetup();
                ads.ApplicationBase = Path.GetPathRoot(_fi.FullName);
                if(Path.GetPathRoot(AppDomain.CurrentDomain.SetupInformation.ApplicationBase) != ads.ApplicationBase
                    || !_extradlls.All((s) => Path.GetPathRoot(s) == ads.ApplicationBase)) {
                    throw new Exception("Installation on multiple drives?--not supported");
                }
                ads.PrivateBinPath = RelativePath(ads.ApplicationBase, Path.GetDirectoryName(_fi.FullName)) + ';'
                    + _extradlls.Select((s) => RelativePath(ads.ApplicationBase, Path.GetDirectoryName(s))).Aggregate((a, s) => a + ';' + s) + ';'
                    + RelativePath(ads.ApplicationBase, AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
                ads.PrivateBinPathProbe = "only";
                ads.DisallowBindingRedirects = false; // ??? from MS example code in AppDomain class description
                ads.DisallowCodeDownload = true;
                ads.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                _domain = AppDomain.CreateDomain("Expr<->" + EngName + " glue", null, ads);
                // this apparently gets a proxy of the created object
                Interface = (StringEngine)_domain.CreateInstanceAndUnwrap(_assyname, _typename);
                Interface.Activate();
            }
            public void Deactivate() {
                if(Interface != null) {
                    Interface.Deactivate();
                    Interface = null;
                }
                if(_domain != null) {
                    try {
                        AppDomain.Unload(_domain);
                    } catch(CannotUnloadAppDomainException) {
                        // ignore; we're probably in a finalizer
                    }
                    _domain = null;
                }
            }
        }
        private List<StringEngineRecord> _records = new List<StringEngineRecord>();
        public StringEngineProxy() {
            DirectoryInfo rundir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
            List<StringEngine> engines = new List<StringEngine>();
            foreach(FileInfo fi in rundir.GetFiles("*Engines.dll")) {
                try {
                    Assembly assy = Assembly.LoadFrom(fi.FullName);
                    foreach(Type t in assy.GetExportedTypes()) {
                        if(!t.IsAbstract && typeof(StringEngine).IsAssignableFrom(t)) {
                            StringEngineRecord r = new StringEngineRecord(t, fi);
                            if(r.Valid) _records.Add(r);
                        }
                    }
                } catch(BadImageFormatException) {
                    // do nothing. The dll isn't in the right format, so we'll ignore it.
                } catch(FileLoadException) {
                    // do nothing. We tried to load the same assembly twice or the assembly name was longer than MAX_PATH characters.
                    // In either case, we can't do anything with it.
                } catch(System.Reflection.ReflectionTypeLoadException) {
                    // ignore ReflectionTypeLoadException
                } catch(TargetInvocationException) {
                    // ignore
                } catch(TypeLoadException) {
                    // ignore; not sure what it is
                } catch(Exception e) {
                    throw new ApplicationException("Could not load special math engine plugin " + fi.Name + ":\n\n" + e.Message, e);
                }
            }
        }

        private static string RelativePath(string root, string path) {
            Debug.Assert(path.StartsWith(root, StringComparison.CurrentCultureIgnoreCase));
            return path.Substring(root.Length + (path[root.Length] == '\\' ? 1 : 0));
        }

        public override Expr _Simplify(Expr e) {
            string mathml = _mml.Convert(e).OuterXml;
            string result = _records[_variant].Interface.Simplify(mathml);
            Expr res = MathML.Convert(result);
            return res;
        }

        public override Expr _Approximate(Expr e) {
            string mathml = _mml.Convert(e).OuterXml;
            string result = _records[_variant].Interface.Approximate(mathml);
            Expr res = MathML.Convert(result);
            return res;
        }

        public override Expr _Substitute(Expr e, Expr orig, Expr replacement) { return (new BuiltInEngine())._Substitute(e, orig, replacement); }
        public override Expr _Substitute(Expr e, MathConstant[] consts) {
            throw new NotImplementedException();
        }
        public override Expr _Replace(Expr e, Expr orig, Expr replacement) { return (new BuiltInEngine())._Replace(e, orig, replacement); }

        public override string Name {
            get { return "Special"; }
        }
        public override string[] Names {
            get {
                return _records.Select((r) => r.EngName).ToArray();
            }
        }
        private int _variant = 0;
        public override int Variant {
            get {
                return _variant;
            }
            set {
                Deactivate();
                _variant = value;
            }
        }

        public override void Activate() {
            _records[_variant].Activate();
        }

        public override void Deactivate() {
            if(_records.Count > 0) _records[_variant].Deactivate();
        }
        ~StringEngineProxy() {
            Deactivate();
        }
    }
}
