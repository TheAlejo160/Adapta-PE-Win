using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace AdaptaPE.Core
{
    public static class AppNames
    {
        static readonly string[][] aliases={
            new[]{"bloc de notas","notepad","bloc","block de notas","bloque de notas"},
            new[]{"google chrome","chrome","crom","gugul crom","google crom","google","gugul","crome","cromo","gugel chrome"},
            new[]{"microsoft edge","edge","msedge","edch","microsoft edch"},
            new[]{"visual studio code","vscode","vs code","code","visual estudio code"},
            new[]{"microsoft word","word","winword"},
            new[]{"microsoft excel","excel"},new[]{"microsoft powerpoint","powerpoint","power point","powerpnt"},
            new[]{"explorador de archivos","explorador","explorer","explorador de windows"},
            new[]{"calculadora","calc","calculator","calculatorapp"},new[]{"paint","mspaint"},
            new[]{"whatsapp","whats app","wasap"},new[]{"firefox","mozilla firefox","fire fox"},
            new[]{"spotify","spotifai"},new[]{"adapta pe","adapta","adaptape"},
            new[]{"microsoft outlook","outlook"},new[]{"microsoft teams","teams","ms-teams","equipos"},
            new[]{"telegram","telegram desktop"},new[]{"discord","discordia"},new[]{"zoom","zoom workplace"},
            new[]{"vlc","vlc media player","videolan"},new[]{"obs studio","obs","obs64"},
            new[]{"adobe acrobat","acrobat","acrord32","acrobat reader","adobe reader"},
            new[]{"brave","brave browser"},new[]{"opera","opera browser"},new[]{"vivaldi","vivaldi browser"},
            new[]{"microsoft onenote","onenote","one note"},new[]{"notepad++","notepad plus plus","notepad mas mas"},
            new[]{"steam","stim"},new[]{"epic games launcher","epic games","epicgameslauncher"},
            new[]{"administrador de tareas","taskmgr","gestor de tareas"},new[]{"recortes","snippingtool","herramienta recortes"},
            new[]{"configuracion","systemsettings","ajustes de windows"},new[]{"microsoft store","winstore.app","tienda de windows"},
            new[]{"wordpad","write"},new[]{"photoshop","adobe photoshop"},new[]{"audacity","audaciti"},
            new[]{"7-zip","7zfm","7 zip"},new[]{"winrar","win rar"}
        };
        public static string[] SpokenNames {get{return aliases.SelectMany(a=>a).Distinct().ToArray();}}
        public static string Clean(string text){
            string n=Commands.Normalize(text).Trim(' ','.',',','!','?','"','«','»');
            n=Regex.Replace(n,@"^(?:por favor[, ]+)|(?:\s+por favor)$","");
            return Regex.Replace(n,@"^(?:(?:el|la|este|esta|ese|esa|mi)\s+|(?:aplicacion|app|programa|navegador)\s+|(?:pagina|sitio|web)\s+(?:de\s+)?)+","").Trim();
        }
        public static string Canonical(string text){string n=Regex.Replace(Clean(text),@"\.exe$","");var group=aliases.FirstOrDefault(a=>a.Contains(n));return group==null?n:group[0];}
        public static int MatchRank(string query,string name) {
            string n=Canonical(query),candidate=Canonical(name);if(n.Length==0||candidate.Length==0)return 0;
            if(n==candidate)return 3;
            var group=aliases.FirstOrDefault(a=>a[0]==candidate);string[] candidates=group??new[]{candidate};
            if(n.Length>=3&&candidates.Any(c=>Regex.IsMatch(c,@"(?<![\p{L}\p{N}])"+Regex.Escape(n)+@"(?![\p{L}\p{N}])")))return 2;
            // ponytail: distancia acotada a nombres completos; ampliar sólo con evidencia de errores reales.
            return n.Length>=4&&candidates.Any(c=>Distance(n,c)<=(n.Length<6?1:2))?1:0;
        }
        static int Distance(string n,string candidate) {
            if(Math.Abs(n.Length-candidate.Length)>2)return 3;
            var row=Enumerable.Range(0,candidate.Length+1).ToArray();
            for(int i=1;i<=n.Length;i++){int previous=row[0];row[0]=i;for(int j=1;j<=candidate.Length;j++){int old=row[j];row[j]=Math.Min(Math.Min(row[j]+1,row[j-1]+1),previous+(n[i-1]==candidate[j-1]?0:1));previous=old;}}
            return row[candidate.Length];
        }
        // Prioriza identidad de proceso; una página que menciona Word no es Microsoft Word.
        public static int RunningRank(string query,string process,string title){
            string n=Canonical(query),p=Canonical(process),t=Clean(title);if(n.Length==0)return 0;
            if(n==p)return 3;
            if(n=="navegador"&&new[]{"google chrome","microsoft edge","firefox","brave","opera","vivaldi"}.Contains(p))return 1;
            var group=aliases.FirstOrDefault(a=>a[0]==n);
            if(group!=null&&Clean(process)!="applicationframehost")return 0;
            string[] names=group??new[]{n};
            if(names.Any(a=>t==a||t.EndsWith(" - "+a,StringComparison.Ordinal)||t.EndsWith(" — "+a,StringComparison.Ordinal)||t.EndsWith(" – "+a,StringComparison.Ordinal)))return 2;
            if(group!=null)return 0;
            // Los parecidos se presentan para elegir; nunca acreditan un cierre exacto.
            if(MatchRank(n,p)>0)return 1;
            if(aliases.Any(a=>a[0]==p))return 0;
            return MatchRank(n,t)>0?1:0;
        }
    }
}
