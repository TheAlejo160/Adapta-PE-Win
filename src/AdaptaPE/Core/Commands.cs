using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AdaptaPE.Core
{
    public sealed class Command
    {
        public string Kind, Text, Target, Destination;
        public Command(string kind, string text = "", string target = "", string destination = "actual")
        { Kind = kind; Text = text; Target = target; Destination = destination; }
    }
    public sealed class Site
    {
        public string Id, Home, Search;
        public string[] Aliases;
        public Site(string id, string home, string search, params string[] aliases)
        { Id = id; Home = home; Search = search; Aliases = aliases; }
        public string Query(string query) { return Search != null ? Search + Uri.EscapeDataString(query) : "https://www.google.com/search?q=" + Uri.EscapeDataString("site:" + new Uri(Home).Host + " " + query); }
    }
    public static class Commands
    {
        public static string Normalize(string text)
        {
            var b = new StringBuilder();
            foreach (char c in (text ?? "").Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) b.Append(char.ToLowerInvariant(c));
            return Regex.Replace(b.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ").Trim();
        }
        static Match Match(string text, string pattern) { return Regex.Match(text, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)); }
        public static readonly Site[] Sites = {
            new Site("google","https://www.google.com/","https://www.google.com/search?q=","google"),
            new Site("youtube","https://www.youtube.com/","https://www.youtube.com/results?search_query=","youtube","you tube"),
            new Site("mercadolibre","https://www.mercadolibre.com.pe/","https://listado.mercadolibre.com.pe/","mercado libre","mercadolibre"),
            new Site("amazon","https://www.amazon.com/","https://www.amazon.com/s?k=","amazon"),
            new Site("wikipedia","https://es.wikipedia.org/","https://es.wikipedia.org/wiki/Especial:Buscar?search=","wikipedia"),
            new Site("facebook","https://www.facebook.com/","https://www.facebook.com/search/top/?q=","facebook","face book"),
            new Site("instagram","https://www.instagram.com/",null,"instagram"),
            new Site("x","https://x.com/","https://x.com/search?q=","x","twitter","equis"),
            new Site("reddit","https://www.reddit.com/","https://www.reddit.com/search/?q=","reddit"),
            new Site("netflix","https://www.netflix.com/","https://www.netflix.com/search?q=","netflix"),
            new Site("twitch","https://www.twitch.tv/","https://www.twitch.tv/search?term=","twitch"),
            new Site("github","https://github.com/","https://github.com/search?q=","github","git hub"),
            new Site("chatgpt","https://chatgpt.com/",null,"chatgpt","chat gpt"), new Site("claude","https://claude.ai/",null,"claude"),
            new Site("gemini","https://gemini.google.com/",null,"gemini"), new Site("canvas","https://canvas.instructure.com/",null,"canvas"),
            new Site("gmail","https://mail.google.com/",null,"gmail","g mail","correo de google"),
            new Site("outlook","https://outlook.live.com/",null,"outlook","hotmail"),
            new Site("whatsapp","https://web.whatsapp.com/",null,"whatsapp","whats app","wasap"),new Site("telegram","https://web.telegram.org/",null,"telegram"),
            new Site("drive","https://drive.google.com/",null,"drive","google drive"), new Site("documentos","https://docs.google.com/",null,"google docs","documentos de google"),
            new Site("mapas","https://www.google.com/maps/","https://www.google.com/maps/search/?api=1&query=","mapas","maps","google maps"),
            new Site("linkedin","https://www.linkedin.com/",null,"linkedin","linked in"),new Site("tiktok","https://www.tiktok.com/",null,"tiktok","tik tok"),
            new Site("spotify","https://open.spotify.com/",null,"spotify"),new Site("bing","https://www.bing.com/","https://www.bing.com/search?q=","bing"),
            new Site("duckduckgo","https://duckduckgo.com/","https://duckduckgo.com/?q=","duckduckgo","duck duck go")
        };
        public static Site FindSite(string name) { var n = Normalize(name); return Sites.FirstOrDefault(s => s.Id == n || s.Aliases.Contains(n)); }
        public static readonly Dictionary<string,string> Shortcuts = new Dictionary<string,string> {
            {"guardar","CTRL+S"},{"guarda","CTRL+S"},{"guardar como","CTRL+SHIFT+S"},{"imprimir","CTRL+P"},
            {"copiar","CTRL+C"},{"copia","CTRL+C"},{"cortar","CTRL+X"},{"pegar","CTRL+V"},{"pega","CTRL+V"},
            {"selecciona todo","CTRL+A"},{"seleccionar todo","CTRL+A"},{"deshacer","CTRL+Z"},{"rehacer","CTRL+Y"},
            {"nueva pestana","CTRL+T"},{"abre nueva pestana","CTRL+T"},{"nueva ventana","CTRL+N"},{"abre nueva ventana","CTRL+N"},
            {"cierra esta pestana","CTRL+W"},{"cierra pestana","CTRL+W"},{"cierra la pestana","CTRL+W"},
            {"cerrar pestana","CTRL+W"},{"cerrar la pestana","CTRL+W"},{"cerrar esta pestana","CTRL+W"},
            {"cierra ventana","ALT+F4"},{"cierra la ventana","ALT+F4"},{"atras","ALT+LEFT"},{"volver","ALT+LEFT"},
            {"cerrar ventana","ALT+F4"},{"cerrar la ventana","ALT+F4"},{"cerrar esta ventana","ALT+F4"},
            {"adelante","ALT+RIGHT"},{"recarga","CTRL+R"},{"recargar","CTRL+R"},{"actualiza la pagina","CTRL+R"},
            {"siguiente pestana","CTRL+TAB"},{"anterior pestana","CTRL+SHIFT+TAB"},{"aumenta zoom","CTRL+PLUS"},{"reduce zoom","CTRL+MINUS"},
            {"restablece zoom","CTRL+0"},{"zoom normal","CTRL+0"},{"buscar en la pagina","CTRL+F"},{"buscar en el documento","CTRL+F"},
            {"barra de direcciones","CTRL+L"},{"campo siguiente","TAB"},{"campo anterior","SHIFT+TAB"},{"tabulador","TAB"},
            {"enter","ENTER"},{"intro","ENTER"},{"escape","ESC"},{"mostrar escritorio","WIN+D"},{"muestra escritorio","WIN+D"},
            {"cambiar aplicacion","ALT+TAB"},{"vista de tareas","WIN+TAB"},{"mission control","WIN+TAB"},{"spotlight","WIN+S"},
            {"buscar en windows","WIN+S"},{"minimizar ventana","WIN+DOWN"},{"pantalla completa","F11"},{"salir de pantalla completa","F11"},
            {"inicio","CTRL+HOME"},{"final","CTRL+END"},{"subir","SCROLL_UP"},{"sube","SCROLL_UP"},{"arriba","SCROLL_UP"},
            {"bajar","SCROLL_DOWN"},{"baja","SCROLL_DOWN"},{"abajo","SCROLL_DOWN"}
        };
        // Frases finitas para SAPI; el dictado libre permanece en otra gramática.
        public static readonly Dictionary<string,string> NaturalShortcuts = new Dictionary<string,string> {
            {"guarda el archivo","CTRL+S"},{"guarda el documento","CTRL+S"},{"guardar archivo","CTRL+S"},{"guarda como","CTRL+SHIFT+S"},
            {"imprime","CTRL+P"},{"imprime el documento","CTRL+P"},{"corta","CTRL+X"},{"copialo","CTRL+C"},{"pegalo","CTRL+V"},
            {"deshaz","CTRL+Z"},{"deshaz eso","CTRL+Z"},{"deshacer lo ultimo","CTRL+Z"},{"rehaz","CTRL+Y"},
            {"selecciona todo el texto","CTRL+A"},{"seleccionar todo el texto","CTRL+A"},
            {"abre otra pestana","CTRL+T"},{"abre otra ventana","CTRL+N"},{"cierra esto","ALT+F4"},{"cerrar","ALT+F4"},{"cierra","ALT+F4"},
            {"cierra la aplicacion actual","ALT+F4"},{"cierra esta aplicacion","ALT+F4"},{"cierra el programa actual","ALT+F4"},
            {"recupera la pestana","CTRL+SHIFT+T"},{"reabre la pestana","CTRL+SHIFT+T"},{"restaura la pestana cerrada","CTRL+SHIFT+T"},
            {"abre el historial","CTRL+H"},{"muestra historial","CTRL+H"},{"abre descargas","CTRL+J"},{"muestra descargas","CTRL+J"},
            {"agrega a favoritos","CTRL+D"},{"guarda en favoritos","CTRL+D"},{"acerca","CTRL+PLUS"},{"aleja","CTRL+MINUS"},
            {"aumentar zoom","CTRL+PLUS"},{"disminuye zoom","CTRL+MINUS"},{"quita el zoom","CTRL+0"},
            {"busca en la pagina","CTRL+F"},{"busca en el documento","CTRL+F"},{"buscar texto","CTRL+F"},
            {"ve al inicio","CTRL+HOME"},{"ve al final","CTRL+END"},{"pagina arriba","PAGEUP"},{"pagina abajo","PAGEDOWN"},
            {"sube la pagina","SCROLL_UP"},{"baja la pagina","SCROLL_DOWN"},{"desplaza hacia arriba","SCROLL_UP"},{"desplaza hacia abajo","SCROLL_DOWN"},
            {"muestra el escritorio","WIN+D"},{"ver escritorio","WIN+D"},{"cambia de ventana","ALT+TAB"},{"cambia de aplicacion","ALT+TAB"},
            {"maximiza la ventana","WIN+UP"},{"maximizar ventana","WIN+UP"},{"minimiza la ventana","WIN+DOWN"},
            {"ventana a la izquierda","WIN+LEFT"},{"ventana a la derecha","WIN+RIGHT"},{"abre el explorador","WIN+E"},
            {"presiona enter","ENTER"},{"pulsa enter","ENTER"},{"presiona escape","ESC"},{"pulsa tabulador","TAB"}
        };
        public static readonly Dictionary<string,string> NaturalActions = new Dictionary<string,string> {
            {"lee pantalla","readScreen"},{"leer la pantalla","readScreen"},{"lee esta pantalla","readScreen"},{"leeme la pantalla","readScreen"},
            {"leeme pantalla","readScreen"},{"lee lo que hay en pantalla","readScreen"},{"lee lo que hay en la pantalla","readScreen"},
            {"lee todo lo que ves","readScreen"},{"que dice la pantalla","readScreen"},{"leas la pantalla","readScreen"},{"leas pantalla","readScreen"},{"lee todo","readScreen"},{"leer todo","readScreen"},{"lee la pagina","readScreen"},{"leeme la ventana","readScreen"},
            {"leer la ventana","readScreen"},{"lee esta ventana","readScreen"},{"lee el contenido","readScreen"},
            {"haz click","click"},{"hacer clic","click"},{"hacer click","click"},{"da clic","click"},{"clic izquierdo","click"},
            {"haz clic izquierdo","click"},{"click izquierdo","click"},{"pulsa el boton izquierdo","click"},
            {"hazme clic derecho","rightClick"},{"hazme un clic derecho","rightClick"},{"click secundario","rightClick"},{"hacer clic derecho","rightClick"},{"hacer click derecho","rightClick"},{"da clic derecho","rightClick"},
            {"dale clic derecho","rightClick"},{"dale click derecho","rightClick"},{"boton derecho","rightClick"},
            {"pulsa el boton derecho","rightClick"},{"abre el menu contextual","rightClick"},{"abrir menu contextual","rightClick"},
            {"clic con el boton derecho","rightClick"},{"haz un clic derecho","rightClick"},{"haz un click derecho","rightClick"},
            {"doble clic","doubleClick"},{"doble click","doubleClick"},{"haz doble clic","doubleClick"},{"haz doble click","doubleClick"},
            {"deja de leer la pantalla","silence"},{"para de leer","silence"},{"deten la lectura","silence"},{"parar lectura","silence"},
            {"detener lectura","silence"},{"callate","silence"},{"no leas mas","silence"},{"para de hablar","silence"},
            {"para la asistencia","stop"},{"detener asistencia","stop"},{"apaga todo","stop"},{"para todas las funciones","stop"},
            {"ya no hables","silence"},{"cancela eso","cancel"},{"cancela la accion","cancel"},{"olvida eso","cancel"},
            {"que aplicaciones estan abiertas","listApps"},{"muestra las aplicaciones abiertas","listApps"},
            {"muestra apps abiertas","listApps"},{"lista aplicaciones abiertas","listApps"},{"que programas estan abiertos","listApps"},
            {"muestra los campos","fields"},{"lista campos","fields"},{"muestra botones","actions"},{"muestra los comandos","actions"},
            {"que puedo pulsar","actions"},{"que acciones hay","actions"},{"limpia el texto","clear"},{"borra todo el campo","clear"}
        };
        public static string[] SpeechPhrases {
            get { return Shortcuts.Keys.Concat(NaturalShortcuts.Keys).Concat(NaturalActions.Keys).Concat(new[]{
                "detener todo","cancelar","silencio","lee la pantalla","muestra campos","muestra comandos","clic","clic derecho",
                "mostrar ajustes","modo flotante","quitar filtro","sin filtro","elige uno","elige dos","elige tres","elige cuatro","elige cinco","elige seis","siguiente","anterior"
            }).Distinct().ToArray(); }
        }
        static string Request(string text) {
            return Regex.Replace((text??"").Trim().TrimStart('¿','¡'),
                @"^(?:(?:(?:por favor|porfa)[, ]+|oye[, ]+|a ver[, ]+|(?:me )?(?:puedes|podr[ií]as)\s+|(?:quiero|necesito)\s+(?:que\s+)?|(?:hazme|haz) el favor de\s+))+", "", RegexOptions.IgnoreCase);
        }
        public static Command Parse(string text)
        {
            string original = Request(text);
            string n; Match m;
            if ((m=Match(original,@"^(?:escribe|escribir|dicta|dictar|introduce) (.+?) en (?:el )?campo (.+)$")).Success) return new Command("writeField",m.Groups[1].Value,m.Groups[2].Value);
            if ((m=Match(original,@"^(?:escribe|escribir|dicta|dictar|introduce|ingresa texto|escr[ií]beme|pon el texto)\s+(.+)$")).Success) return new Command("dictate",m.Groups[1].Value);
            if ((m=Match(original,@"^(?:pregunta|consulta)\s+a\s+la\s+ia\s+(.+)$")).Success) return new Command("ai",m.Groups[1].Value);
            original=Regex.Replace(original,@"[, ]+(?:por favor|porfa)[.!?]*$","",RegexOptions.IgnoreCase).TrimEnd('.', '!', '?');
            n=Normalize(original);
            string natural;
            if(NaturalActions.TryGetValue(n,out natural))return new Command(natural);
            if(NaturalShortcuts.TryGetValue(n,out natural))return new Command("shortcut",natural);
            if((m=Match(n,@"^(?:activa|activar|enciende|encender|desactiva|desactivar|apaga|apagar) (?:la )?(?:voz|escucha|microfono)$")).Success)return new Command("voice",n.StartsWith("desact")||n.StartsWith("apag")?"off":"on");
            if (n=="analiza pantalla con ia") return new Command("screenAI");
            if ((m=Match(original,@"^(?:busca|buscar|b[uú]squeda de|encuentra|consulta|investiga|b[uú]scame|averigua)\s+(.+)$")).Success) {
                string q=m.Groups[1].Value; string dest=Destination(ref q); string normalized=Normalize(q);
                foreach(var s in Sites) foreach(var a in s.Aliases.OrderByDescending(x=>x.Length)) {
                    if(normalized.EndsWith(" en "+a,StringComparison.Ordinal)) return new Command("search",q.Substring(0,q.Length-a.Length-4),s.Id,dest);
                    if(normalized.StartsWith("en "+a+" ",StringComparison.Ordinal)) return new Command("search",q.Substring(a.Length+4),s.Id,dest);
                }
                return new Command("search",q,"",dest);
            }
            if((m=Match(n,@"^(?:(?:abre|abrir|crea|crear) )?(?:una )?(?:nueva (pestana|ventana)|(pestana|ventana)(?: nueva)?)$")).Success)return new Command("shortcut",n.Contains("ventana")?"CTRL+N":"CTRL+T");
            if((m=Match(n,@"^(?:cierra|cerrar) (?:(?:esta|la|actual) )?(?:pestana|ventana)(?: actual)?$")).Success)return new Command("shortcut",n.Contains("ventana")?"ALT+F4":"CTRL+W");
            if(new[]{"regresa","regresar","retrocede","retroceder","pagina anterior","vuelve atras","volver atras"}.Contains(n))return new Command("shortcut","ALT+LEFT");
            if(new[]{"avanza","pagina siguiente","ve adelante"}.Contains(n))return new Command("shortcut","ALT+RIGHT");
            if((m=Match(n,@"^(?:recarga|recargar|actualiza|actualizar|refresca|refrescar)(?: la)?(?: pagina)?$")).Success)return new Command("shortcut","CTRL+R");
            if((m=Match(n,@"^(?:cambia (?:a )?(?:la )?)?(?:siguiente pestana|pestana siguiente|anterior pestana|pestana anterior)$")).Success)return new Command("shortcut",n.Contains("anterior")?"CTRL+SHIFT+TAB":"CTRL+TAB");
            if(new[]{"centrar cursor","postura comoda","acomodar postura"}.Contains(n))return new Command("calibrate");
            if(n=="descansar"||n=="continuar cursor")return new Command("pause",n=="descansar"?"on":"off");
            if(n=="cancelar clic")return new Command("cancelClick");
            if(n=="control rostro")return new Command("mode","cabeza");
            if(n=="control munon")return new Command("mode","zona libre");
            if(n=="movilidad reducida")return new Command("small","on");
            if(n=="ajustar temblor")return new Command("tremor","on");
            if(n=="detener todo"||n=="deten todo"||n=="para todo") return new Command("stop");
            if(new[]{"cancelar","cancela","olvidalo","no hagas nada","cancelar seleccion"}.Contains(n)) return new Command("cancel");
            if(new[]{"silencio","callar","deja de leer","deja de hablar"}.Contains(n)) return new Command("silence");
            if(new[]{"lee la pantalla","leer pantalla","lee la ventana","ojo bionico"}.Contains(n)) return new Command("readScreen");
            if(new[]{"muestra campos","mostrar campos","busca campos de texto"}.Contains(n)) return new Command("fields");
            if(new[]{"muestra comandos","mostrar comandos","muestra acciones","acciones disponibles"}.Contains(n)) return new Command("actions");
            if(new[]{"pulsa lo seleccionado","dale clic","dale click","hazle clic"}.Contains(n)) return new Command("pressSelection");
            if(new[]{"borra el campo","borra el texto","limpia el campo","borrar campo"}.Contains(n)) return new Command("clear");
            if(new[]{"clic","click","dar clic","haz clic"}.Contains(n)) return new Command("click");
            if(new[]{"clic derecho","click derecho","anticlick","anticlic","dar anticlick","anti click","menu contextual","haz clic derecho","haz click derecho","clic secundario","siguiente clic derecho","activar clic derecho"}.Contains(n)) return new Command(n.StartsWith("siguiente")||n=="activar clic derecho"?"nextRight":"rightClick");
            if(n=="reproduce"||n=="reproduce el video"||n=="continuar video") return new Command("media","reproducir");
            if(n=="pausa"||n=="pausa el video"||n=="pausar audio") return new Command("media","pausar");
            if((m=Match(n,@"^(?:filtro|activar filtro)\s+(.+)$")).Success) return new Command("filter",m.Groups[1].Value);
            if(n=="quitar filtro"||n=="sin filtro") return new Command("filter","ninguno");
            if(n=="activar talkback"||n=="desactivar talkback") return new Command("talkback",n.StartsWith("activar")?"on":"off");
            if((m=Match(n,@"^(?:calibrar|calibra|recalibrar|recalibra)(?: (?:el )?cursor)?$")).Success)return new Command("calibrate");
            if(n.StartsWith("control ")) return new Command("mode",n.Substring(8));
            if((m=Match(n,@"^(activar|activa|enciende|desactivar|desactiva|apaga) (?:el )?(?:cursor|mouse cinetico|raton cinetico)$")).Success)return new Command("camera",new[]{"activar","activa","enciende"}.Contains(m.Groups[1].Value)?"on":"off");
            if((m=Match(n,@"^(pausar|pausa|reanudar|reanuda) (?:el )?cursor$")).Success)return new Command("pause",m.Groups[1].Value.StartsWith("paus")?"on":"off");
            if(n=="activar temblor"||n=="desactivar temblor") return new Command("tremor",n.StartsWith("activar")?"on":"off");
            if(n=="movimientos pequenos"||n=="movimientos normales") return new Command("small",n.Contains("pequenos")?"on":"off");
            if((m=Match(n,@"^(?:(?:cursor|mouse|raton)\s+mas\s+(lento|rapido)|(?:aumenta|sube|reduce|baja)\s+(?:la )?(?:velocidad|sensibilidad)(?: del (?:cursor|mouse|raton))?)$")).Success) return new Command("speed",n.Contains("lento")||n.StartsWith("reduce")||n.StartsWith("baja")?"down":"up");
            if(n=="mas rapido"||n=="mas lento")return new Command("speed",n.EndsWith("lento")?"down":"up");
            if(n=="cursor preciso"||n=="modo preciso"||n=="cursor rapido"||n=="modo rapido"||n=="perfil agil")return new Command("cursorProfile",n.Contains("preciso")?"preciso":"agil");
            if((m=Match(n,@"^(activar|activa|desactivar|desactiva) (?:el )?(?:clic automatico|click automatico|dwell)$")).Success)return new Command("dwell",m.Groups[1].Value.StartsWith("activa")?"on":"off");
            if(new[]{"mostrar ajustes","abrir adapta","modo flotante","ocultar adapta","salir de adapta"}.Contains(n)) return new Command("app",n);
            if(n.StartsWith("panel esquina ")) return new Command("corner",n.Substring(14));
            if((m=Match(original,@"^(?:elige|elijo|escoge|escojo|(?:la )?opci[oó]n|n[uú]mero)\s+(.+)$")).Success) return new Command("choose",m.Groups[1].Value);
            if(Ordinal(n)>=0||n=="siguiente"||n=="anterior") return new Command("choose",n);
            if((m=Match(original,@"^(?:selecciona|seleccionar|resalta) (?:el )?texto\s+(.+)$")).Success) return new Command("selectText",m.Groups[1].Value);
            if((m=Match(original,@"^(selecciona|seleccionar|resalta|enfoca|enfocar|lee|leer|l[eé]eme|pulsa|presiona|clic en|click en|haz clic en|haz click en|dale clic a|dale click a)\s+(?:(?:el|la) )?(?:(?:bot[oó]n|campo|enlace|elemento) )?(.+)$")).Success) {
                string verb=Normalize(m.Groups[1].Value);
                if(verb=="presiona" && KeyNames.TryParse(m.Groups[2].Value)!=null) return new Command("keys",m.Groups[2].Value);
                return new Command(verb.StartsWith("seleccion")||verb=="resalta"?"select":verb.StartsWith("enfoc")?"focus":verb.StartsWith("le")?"read":"press",m.Groups[2].Value);
            }
            if((m=Match(original,@"^(?:atajo|ejecuta el atajo|presiona el atajo)\s+(.+)$")).Success) return new Command("keys",m.Groups[1].Value);
            if((m=Match(original,@"^(?:ejecuta|activa|pulsa) (?:la )?(?:acci[oó]n|opci[oó]n de men[uú])\s+(.+)$")).Success) return new Command("menu",m.Groups[1].Value);
            string shortcut; if(Shortcuts.TryGetValue(n,out shortcut)) return new Command("shortcut",shortcut);
            if((m=Match(n,@"^(?:cambia|cambiar|ve|vuelve|volver|pasate) (?:a|al) (.+)$")).Success)return new Command("switchApp",AppNames.Clean(m.Groups[1].Value));
            if((m=Match(n,@"^(?:trae|traeme) (.+?)(?: al frente)?$")).Success)return new Command("switchApp",AppNames.Clean(m.Groups[1].Value));
            if((m=Match(n,@"^(?:abre )?(?:una )?(nueva ventana|nueva pestana|ventana nueva|pestana nueva) (?:de|en) (.+)$")).Success) return new Command("appShortcut",m.Groups[1].Value.Contains("ventana")?"CTRL+N":"CTRL+T",m.Groups[2].Value);
            if((m=Match(original,@"^(?:abre|abrir|abras|[aá]breme|ejecuta|ejecutar|inicia|iniciar|lanza|lanzar|muestra|ponme|entra a|entra en|visita)\s+(.+)$")).Success) {
                string target=m.Groups[1].Value; string dest=Destination(ref target);bool site=Regex.IsMatch(Normalize(target),@"^(?:(?:el|la) )?(?:pagina|sitio|web) (?:de )?");
                if(site)target=Regex.Replace(target,@"^(?:(?:el|la) )?(?:p[aá]gina|sitio|web) (?:de )?","",RegexOptions.IgnoreCase);
                return new Command("open",target,site?"site":"",dest);
            }
            if((m=Match(n,@"^(?:cierra|cerrar|cierres|cierre|cierrame|sierra|termina|terminar|cierra por favor|cerrar por favor|sal de|salir de)\s+(.+)$")).Success) return new Command("closeApp",AppNames.Clean(m.Groups[1].Value));
            return new Command("unknown",original);
        }
        static string Destination(ref string text) {
            var m=Match(text,@"\s+(?:(?:en|en una|en otra)\s+)?(?:nueva (?:pesta[nñ]a|ventana)|(?:pesta[nñ]a|ventana) nueva)$");
            if(!m.Success)return "actual"; string d=Normalize(m.Value).Contains("ventana")?"ventana":"pestana"; text=text.Substring(0,m.Index); return d;
        }
        public static int Ordinal(string text) {
            string n=Normalize(text); int i; if(int.TryParse(n,out i)&&i>0)return i-1;
            var names=new[]{"primera","segunda","tercera","cuarta","quinta","sexta"};
            n=Regex.Replace(n,@"^(?:el|la)\s+", "");
            int cardinal=Array.IndexOf(new[]{"uno","dos","tres","cuatro","cinco","seis"},n);if(cardinal>=0)return cardinal;
            return Array.IndexOf(names,n.Replace("primero","primera").Replace("segundo","segunda").Replace("tercero","tercera").Replace("cuarto","cuarta").Replace("quinto","quinta").Replace("sexto","sexta"));
        }
    }
    public static class KeyNames
    {
        public static ushort[] TryParse(string value) {
            var keys=new List<ushort>();
            foreach(string token in Commands.Normalize(value).Replace("+"," ").Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries)) {
                ushort k;
                switch(token) {
                    case "control":case "ctrl":k=0x11;break; case "shift":case "mayus":case "mayusculas":k=0x10;break;
                    case "alt":k=0x12;break;case "windows":case "win":k=0x5B;break;
                    case "enter":case "intro":k=0x0D;break;case "escape":case "esc":k=0x1B;break;case "tab":case "tabulador":k=9;break;
                    case "espacio":case "space":k=0x20;break;case "arriba":case "up":k=0x26;break;case "abajo":case "down":k=0x28;break;
                    case "izquierda":case "left":k=0x25;break;case "derecha":case "right":k=0x27;break;
                    case "pageup":case "repage":k=0x21;break;case "pagedown":case "avpage":k=0x22;break;
                    case "inicio":case "home":k=0x24;break;case "fin":case "end":k=0x23;break;case "suprimir":case "delete":k=0x2E;break;
                    case "retroceso":case "backspace":k=8;break;case "plus":k=0xBB;break;case "minus":k=0xBD;break;
                    default:
                        int f; if(token.Length==1 && char.IsLetterOrDigit(token[0]) && token[0]<128)k=(ushort)char.ToUpperInvariant(token[0]);
                        else if(token.StartsWith("f")&&int.TryParse(token.Substring(1),out f)&&f>=1&&f<=24)k=(ushort)(0x6F+f);else return null;
                        break;
                }
                if(keys.Contains(k))return null; keys.Add(k);
            }
            return keys.Count>0 && keys.Count<=8?keys.ToArray():null;
        }
    }
    public sealed class WakeState
    {
        double until;
        public void Cancel(){until=0;}
        public string Consume(string text,string word,double time,bool speaking,bool followUp,out bool beep) {
            beep=false; text=(text??"").Trim(); if(text.Length==0)return null;
            var m=Regex.Match(Commands.Normalize(text),@"(?<![\p{L}\p{N}])"+Regex.Escape(Commands.Normalize(string.IsNullOrWhiteSpace(word)?"computadora":word))+@"(?![\p{L}\p{N}])");
            string command=m.Success?text.Substring(m.Index+m.Length).TrimStart(' ',',','.',':',';','!','?'):(!speaking&&(time<until||followUp)?text:null);
            if(command==null)return null;
            if(command.Length==0){until=time+8;beep=true;return null;}
            if(speaking && !new[]{"silence","cancel","stop"}.Contains(Commands.Parse(command).Kind))return null;
            until=0;return command;
        }
    }
}
