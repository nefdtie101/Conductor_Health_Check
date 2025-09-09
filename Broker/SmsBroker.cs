using System.Net.Http.Json;
using Broker.Models.dextel;
using Microsoft.Extensions.Configuration;


namespace Broker;

public class SmsBroker
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private string _url;
    private List<string> _to;
    private string _from;

    
    public SmsBroker(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _config = configuration;
        _url = _config.GetValue<string>("dexatel:url");
        _httpClient.DefaultRequestHeaders.Add("X-Dexatel-Key", _config.GetValue<string>("dexatel:token"));
        _to = _config.GetSection("dexatel:to").Get<List<string>>();
        _from = _config.GetValue<string>("dexatel:from");
    }
    
    
    public async Task<bool> sendSms(string messages)
    {
        try
        {
            var sendData = new sendModel();
            sendData.data = new data();
            sendData.data.channel = "SMS";
            sendData.data.from = _from;
            sendData.data.to = _to;
            sendData.data.text = messages;
            var res = await _httpClient.PostAsJsonAsync(_url + "/v1/messages", sendData);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

}