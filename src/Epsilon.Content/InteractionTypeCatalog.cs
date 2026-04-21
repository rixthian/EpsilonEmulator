namespace Epsilon.Content;

public static class InteractionTypeCatalog
{
    public static readonly IReadOnlySet<string> KnownInteractionTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "default",
        "gate",
        "postit",
        "roomeffect",
        "dimmer",
        "trophy",
        "bed",
        "scoreboard",
        "vendingmachine",
        "alert",
        "onewaygate",
        "loveshuffler",
        "habbowheel",
        "dice",
        "bottle",
        "teleport",
        "rentals",
        "pet",
        "roller",
        "water",
        "ball",
        "pressure_pad",
        "counter",
        "switch",
        "puzzlebox",
        "wired",
        "wf_trg_onsay",
        "wf_trg_enterroom",
        "wf_trg_furnistate",
        "wf_trg_onfurni",
        "wf_trg_offfurni",
        "wf_trg_gameend",
        "wf_trg_gamestart",
        "wf_trg_timer",
        "wf_trg_attime",
        "wf_trg_atscore",
        "wf_act_saymsg",
        "wf_act_moveuser",
        "wf_act_togglefurni",
        "wf_act_givepoints",
        "wf_act_moverotate",
        "wf_act_matchfurni",
        "wf_cnd_trggrer_on_frn",
        "wf_cnd_furnis_hv_avtrs",
        "wf_cnd_has_furni_on"
    };
}

