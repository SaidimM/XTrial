"""Compile scripts and exercise track/progress logic using the installed Tuanjie compiler.
The headless Unity shims cover numerical logic only; they do not replace editor playtesting.
Run: python3 Tools/verify_tracks.py
"""
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
EDITOR = Path('/Applications/Tuanjie/Tuanjie.app/Contents')
MONO = str(EDITOR / 'MonoBleedingEdge/bin/mono')
CSC = str(EDITOR / 'MonoBleedingEdge/lib/mono/4.5/csc.exe')

SHIMS = r'''
using System;
using System.Collections.Generic;
namespace UnityEngine {
 public enum RuntimeInitializeLoadType { SubsystemRegistration }
 public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){} }
 public struct Vector2 {
  public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;}
  public static Vector2 zero => new Vector2(0,0);
  public Vector2 normalized {get{float n=(float)Math.Sqrt(x*x+y*y);return new Vector2(x/n,y/n);}}
  public static float Dot(Vector2 a,Vector2 b)=>a.x*b.x+a.y*b.y;
  public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);
 }
 public struct Vector3 {
  public float x,y,z;public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator *(Vector3 a,float s)=>new Vector3(a.x*s,a.y*s,a.z*s);
 }
 public static class Mathf {
  public const float Deg2Rad=(float)(Math.PI/180);
  public static float Max(float a,float b)=>Math.Max(a,b);
  public static float Min(float a,float b)=>Math.Min(a,b);
  public static int Max(int a,int b)=>Math.Max(a,b);
  public static int Min(int a,int b)=>Math.Min(a,b);
  public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(v,b));
  public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(v,b));
  public static float Abs(float a)=>Math.Abs(a);
  public static float Sin(float a)=>(float)Math.Sin(a);
  public static float Cos(float a)=>(float)Math.Cos(a);
  public static float Sqrt(float a)=>(float)Math.Sqrt(a);
  public static float Exp(float a)=>(float)Math.Exp(a);
  public static float MoveTowards(float v,float t,float d)=>Math.Abs(t-v)<=d?t:v+Math.Sign(t-v)*d;
 }
 public static class PlayerPrefs {
  static Dictionary<string,float> values=new Dictionary<string,float>();
  public static int GetInt(string k,int d)=>(int)GetFloat(k,d);
  public static float GetFloat(string k,float d)=>values.ContainsKey(k)?values[k]:d;
  public static void SetInt(string k,int v){values[k]=v;}
  public static void SetFloat(string k,float v){values[k]=v;}
  public static void Save(){}
 }
}
'''

TEST = r'''
class Test {
 static void Check(bool ok,string description){if(!ok)throw new Exception(description);}
 static void Main(){
  Check(TrialLevels.All.Length==5,"Expected five tracks");
  Check(TrialLevels.UnlockedCount==1,"Only introduction unlocked initially");
  Check(!TrialLevels.Select(-1)&&!TrialLevels.Select(5)&&!TrialLevels.Select(1),"Reject invalid and locked selections");
  Check(!TrialLevels.Complete(1,10),"Locked tracks cannot unlock progress");
  var ids=new HashSet<string>();
  for(int i=0;i<TrialLevels.All.Length;i++){
   var level=TrialLevels.All[i];
   Check(ids.Add(level.Id),"Stable IDs must be unique");
   for(int p=1;p<level.Points.Length;p++)Check(level.Points[p].x>level.Points[p-1].x,"Monotonic track points");
   Check(level.Points[level.Points.Length-1].x>level.FinishX,"Landing area after finish");
   Check(level.Height(TrialLevels.StartX)==0,"Flat safe start");
   for(float x=-20;x<level.FinishX+20;x+=.05f){
    float h=level.Height(x);
    Check(!float.IsNaN(h)&&!float.IsInfinity(h),"Finite terrain");
    Check(Math.Abs(level.Height(x+.001f)-h)<.01,"Continuous terrain");
   }
   Check(Math.Abs(level.Height(level.FinishX)-level.Height(level.FinishX-1))<.01,"Flat finish");
   Check(TrialLevels.Select(i),"Sequential unlock should permit selection");
   Check(!TrialLevels.Complete(i,float.NaN)&&!TrialLevels.Complete(i,0)&&!TrialLevels.Complete(i,float.PositiveInfinity),"Reject invalid times");
   Check(TrialLevels.Complete(i,30+i),"First finish creates record");
   Check(!TrialLevels.Complete(i,40+i),"Slower finish must preserve record");
   Check(TrialLevels.BestTime(i)==30+i,"Correct per-track record");
   Check(TrialLevels.Complete(i,25+i),"Faster finish improves record");
   Check(TrialLevels.UnlockedCount==Math.Min(i+2,5),"Unlock exactly the next track");
  }
  for(int i=0;i<5;i++)Check(TrialLevels.BestTime(i)==25+i,"Records isolated by track");
  Check(TrialLevels.Select(0)&&TrialLevels.UnlockedCount==5,"Replaying earlier track preserves unlocks");
  Console.WriteLine("PASS: track continuity, safe starts/finishes, selection guards, sequential unlocks and per-track best times.");
  PhysicsSmoke.Run();
 }
}
'''

# Reuse the production controller's numerical methods rather than copying its physics.
source = (ROOT / 'Assets/Scripts/BikeController.cs').read_text()
physics = source[source.index('    void PhysicsStep('):source.index('    void UpdateCamera()')]
physics = physics.replace('FindObjectOfType<GameManager>()?.ShowMessage("BACK IN THE SADDLE");', '')
fields = ['gravity', 'mass', 'inertia', 'wheelBase', 'wheelRadius', 'restLength', 'springK',
          'damperC', 'frictionMu', 'engineForce', 'brakeForce', 'maxLeanSpeed', 'leanRampTime',
          'leanReleaseDamping', 'leanTorque', 'helmetContactTolerance', 'crashContactDelay', 'hardCrashSpeed']
initializers = '\n'.join('float '+name+' = '+re.search(r'public float '+name+r'\s*=\s*([^;]+)', source).group(1)+';' for name in fields)
smoke = r'''
class Position {public Vector3 position;}
class TerrainGenerator {
 public TrialLevel level;
 public float GetHeight(float x)=>level.Height(x);
 public float GetSlope(float x)=>GetHeight(x+.5f)-GetHeight(x-.5f);
}
class PhysicsSmoke {
 INITIALIZERS
 Vector2 velocity; float angularVel,bikeAngle,leanInput,rearWheelAngle,frontWheelAngle,headContactTime,bodyContactTime;
 bool crashed; Position transform=new Position(); TerrainGenerator terrain=new TerrainGenerator();
 PHYSICS
 bool Ride(TrialLevel level,float speed,float tilt){
  terrain.level=level;
  transform.position=new Vector3(TrialLevels.StartX,wheelRadius+restLength+.55f,0);
  float dt=1f/120;
  for(int step=0;step<120*100&&!crashed;step++){
   float x=transform.position.x;
   if(x>=level.FinishX)return true;
   float slope=(level.Height(x+.64f)-level.Height(x-.64f))/1.28f;
   float target=(float)Math.Atan(slope)+tilt;
   float error=target-bikeAngle;
   PhysicsStep(dt,velocity.x<speed,false,error>.04f,error<-.04f);
  }
  return false;
 }
 public static void Run(){
  foreach(var level in TrialLevels.All){
   bool completed=false;
   foreach(float tilt in new[]{0f,.1f,.2f,.3f,.4f}) {
    foreach(float speed in new[]{4f,6f,8f,10f})if(new PhysicsSmoke().Ride(level,speed,tilt)){completed=true;break;}
    if(completed)break;
   }
   if(!completed)throw new Exception("No smoke-test ride completed: "+level.Name);
   Console.WriteLine("PASS: headless ride to finish — "+level.Name);
  }
 }
}
'''.replace('INITIALIZERS', initializers).replace('PHYSICS', physics)

with tempfile.TemporaryDirectory(prefix='trialx-check-') as temp:
    temp = Path(temp)
    refs = list((EDITOR / 'Managed/UnityEngine').glob('UnityEngine.*Module.dll'))
    refs += list((EDITOR / 'Managed/UnityEngine').glob('UnityEditor.*Module.dll'))
    refs += [EDITOR / 'NetStandard/ref/2.1.0/netstandard.dll']
    subprocess.run([MONO, CSC, '-nologo', '-nostdlib', '-target:library', '-out:'+str(temp/'Project.dll')]
                   + ['-r:'+str(p) for p in refs] + [str(p) for p in (ROOT/'Assets').rglob('*.cs')], check=True)
    print('PASS: project C# compilation', flush=True)
    harness = temp / 'TrackTests.cs'
    harness.write_text('using UnityEngine;\n' + SHIMS + TEST + smoke)
    subprocess.run([MONO, CSC, '-nologo', '-out:'+str(temp/'TrackTests.exe'),
                    str(harness), str(ROOT/'Assets/Scripts/TrialLevels.cs')], check=True)
    subprocess.run([MONO, str(temp/'TrackTests.exe')], check=True)
