using System.Text;
using ICSharpCode.SharpZipLib.Zip;

// args: <entry_names.txt> <out.zip> <password> [overridesDir]
// Each entry defaults to "[]"; if <overridesDir>/<EntryName> exists, its bytes are used instead.
var namesFile = args[0];
var outPath = args[1];
var password = args[2];
var overridesDir = args.Length > 3 ? args[3] : null;
var emptyBody = Encoding.UTF8.GetBytes("[]");

var names = File.ReadAllLines(namesFile).Where(l => l.Trim().Length > 0).ToArray();
var overridden = 0;

using (var fs = File.Create(outPath))
using (var zip = new ZipOutputStream(fs))
{
    zip.Password = password;         // traditional ZipCrypto, same as the client uses to open
    zip.SetLevel(0);                 // store (fast); SharpZipLib still encrypts
    foreach (var name in names)
    {
        var body = emptyBody;
        if (overridesDir != null)
        {
            var ov = Path.Combine(overridesDir, name);
            if (File.Exists(ov)) { body = File.ReadAllBytes(ov); overridden++; }
        }
        var e = new ZipEntry(name) { DateTime = new DateTime(2020, 1, 1) };
        zip.PutNextEntry(e);
        zip.Write(body, 0, body.Length);
        zip.CloseEntry();
    }
    zip.Finish();
}

Console.WriteLine($"wrote {outPath} : {names.Length} entries ({overridden} overridden), pwd='{password}'");

// self-verify: reopen with the password and read a couple entries
using (var zf = new ZipFile(outPath))
{
    zf.Password = password;
    int ok = 0; string plSample = null;
    foreach (ZipEntry e in zf)
    {
        using var s = zf.GetInputStream(e);
        using var r = new StreamReader(s);
        var txt = r.ReadToEnd();
        if (txt.Length > 0) ok++;
        if (e.Name == "PlayerLevelMaster.json") plSample = $"{e.Name} -> '{txt[..Math.Min(80, txt.Length)]}...'";
    }
    Console.WriteLine($"verify: {ok}/{zf.Count} entries decrypt OK; {plSample}");
}
