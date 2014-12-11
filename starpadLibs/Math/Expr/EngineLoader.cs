using System;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace starPadSDK.MathExpr {
    static public class EngineLoader {
        static public void Init() { }
        static EngineLoader()
        {
			DirectoryInfo rundir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
            List<Engine> engines = new List<Engine>();
			foreach(FileInfo fi in rundir.GetFiles("ExprBackends.dll")) {
				try {
					Assembly assy = Assembly.LoadFrom(fi.FullName);
					foreach(Type t in assy.GetExportedTypes()) {
                        try {
						if(!t.IsAbstract && typeof(Engine).IsAssignableFrom(t)) {
							Engine e = (Engine) Activator.CreateInstance(t);
							if(e.Exists) engines.Add(e);
						}
                        } catch(Exception) {}
					}
				} catch (BadImageFormatException) {
					// do nothing. The dll isn't in the right format, so we'll ignore it.
				} catch (FileLoadException) {
					// do nothing. We tried to load the same assembly twice or the assembly name was longer than MAX_PATH characters.
					// In either case, we can't do anything with it.
				} catch (System.Reflection.ReflectionTypeLoadException) {
					// ignore ReflectionTypeLoadException
                } catch (TypeLoadException) {
                    // ignore
				} catch (Exception e) {
					// throw new ApplicationException("Could not load math engine plugin " + fi.Name + ":\n\n" + e.Message, e);
				}
			}
            engines.Sort(delegate(Engine a, Engine b) { return a.Name.CompareTo(b.Name); });
            engines.Add(new BuiltInEngine());
			Engine.Engines = engines.ToArray();
                
			int ix = 0;
            for (int i = 0; i < Engine.Engines.Length; i++)
            {
                if (Engine.Engines[i] is BuiltInEngine)
                {				
               // if(_engines[i] is MMAEngine) {
					ix = i;
                    Engine.Engines[i].Activate();
					break;
				}
			}
            Engine.Current = Engine.Engines[ix];
		}
	}
}
