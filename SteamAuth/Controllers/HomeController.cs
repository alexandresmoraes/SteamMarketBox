using RestSharp;
using SteamAuth.Models;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SteamAuth.Controllers
{
  public class HomeController : Controller
  {
    private readonly Regex _accountIdRegex = new Regex(@"^http://steamcommunity\.com/openid/id/(7[0-9]{15,25})$", RegexOptions.Compiled);

    public async Task<ActionResult> Index()
    {
      long? steamid = null;

      var authenticateResult =
          await HttpContext.GetOwinContext().Authentication.AuthenticateAsync("ExternalCookie");

      var firstOrDefault = authenticateResult?.Identity.Claims.FirstOrDefault(claim => claim.Issuer == "Steam" && claim.Type.Contains("nameidentifier"));

      var idString = firstOrDefault?.Value;
      var match = _accountIdRegex.Match(idString ?? "");

      if (match.Success)
      {
        var accountId = match.Groups[1].Value;
        steamid = long.Parse(accountId);
      }

      if (steamid != null)
      {
        // http://steamcommunity.com/inventory/76561198077997592/730/2?l=english&count=5000
        var client = new RestClient("http://steamcommunity.com");

        var request = new RestRequest("inventory/{steamId}/{appId}/2", Method.GET);
        request.AddParameter("l", "english");
        request.AddParameter("count", "5000");
        request.AddUrlSegment("appId", 730);
        request.AddUrlSegment("steamId", steamid);

        IRestResponse<InventoryViewModel> response2 = client.Execute<InventoryViewModel>(request);

        ViewData["inventory"] = response2.Data;
      }

      return View(steamid);
    }

    public ActionResult AuthoriseSteam()
    {
      return new ChallengeResult("Steam", Url.Action("Index"));
    }
  }
}