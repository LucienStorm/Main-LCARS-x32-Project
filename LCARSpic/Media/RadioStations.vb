' LCARSpic/Media/RadioStations.vb
Option Strict On
Option Explicit On

''' <summary>Free internet-radio presets (title|stream URL). No API keys required.</summary>
Public Module RadioStations
    Public Structure Station
        Public Title As String
        Public Url As String
        Public Sub New(ByVal title As String, ByVal url As String)
            Me.Title = title
            Me.Url = url
        End Sub
    End Structure

    ''' <summary>Curated free streams: SomaFM, Radio Paradise, and other open Icecast/AAC feeds.</summary>
    Public ReadOnly All As Station() = {
        New Station("SomaFM Groove Salad", "https://ice1.somafm.com/groovesalad-128-mp3"),
        New Station("SomaFM Groove Salad Classic", "https://ice1.somafm.com/gsclassic-128-mp3"),
        New Station("SomaFM Drone Zone", "https://ice1.somafm.com/dronezone-128-mp3"),
        New Station("SomaFM Space Station Soma", "https://ice1.somafm.com/spacestation-128-mp3"),
        New Station("SomaFM Deep Space One", "https://ice1.somafm.com/deepspaceone-128-mp3"),
        New Station("SomaFM Lush", "https://ice1.somafm.com/lush-128-mp3"),
        New Station("SomaFM Indie Pop Rocks", "https://ice1.somafm.com/indiepop-128-mp3"),
        New Station("SomaFM PopTron", "https://ice1.somafm.com/poptron-128-mp3"),
        New Station("SomaFM Left Coast 70s", "https://ice1.somafm.com/seventies-128-mp3"),
        New Station("SomaFM Underground 80s", "https://ice1.somafm.com/u80s-128-mp3"),
        New Station("SomaFM Secret Agent", "https://ice1.somafm.com/secretagent-128-mp3"),
        New Station("SomaFM Beat Blender", "https://ice1.somafm.com/beatblender-128-mp3"),
        New Station("SomaFM Fluid", "https://ice1.somafm.com/fluid-128-mp3"),
        New Station("SomaFM The Trip", "https://ice1.somafm.com/thetrip-128-mp3"),
        New Station("SomaFM DEF CON Radio", "https://ice1.somafm.com/defcon-128-mp3"),
        New Station("SomaFM Metal Detector", "https://ice1.somafm.com/metal-128-mp3"),
        New Station("SomaFM Boot Liquor", "https://ice1.somafm.com/bootliquor-128-mp3"),
        New Station("SomaFM Illinois Street Lounge", "https://ice1.somafm.com/illstreet-128-mp3"),
        New Station("SomaFM Mission Control", "https://ice1.somafm.com/missioncontrol-128-mp3"),
        New Station("SomaFM Sonic Universe", "https://ice1.somafm.com/sonicuniverse-128-mp3"),
        New Station("SomaFM Suburbs of Goa", "https://ice1.somafm.com/suburbsofgoa-128-mp3"),
        New Station("SomaFM ThistleRadio", "https://ice1.somafm.com/thistle-128-mp3"),
        New Station("SomaFM Folk Forward", "https://ice1.somafm.com/folkfwd-128-mp3"),
        New Station("SomaFM SF 10-33", "https://ice1.somafm.com/sf1033-128-mp3"),
        New Station("SomaFM Cliqhop idm", "https://ice1.somafm.com/cliqhop-128-mp3"),
        New Station("SomaFM Digitalis", "https://ice1.somafm.com/digitalis-128-mp3"),
        New Station("SomaFM Dub Step Beyond", "https://ice1.somafm.com/dubstep-128-mp3"),
        New Station("SomaFM BAGeL Radio", "https://ice1.somafm.com/bagel-128-mp3"),
        New Station("SomaFM Seven Inch Soul", "https://ice1.somafm.com/7soul-128-mp3"),
        New Station("SomaFM Vaporwaves", "https://ice1.somafm.com/vaporwaves-128-mp3"),
        New Station("Radio Paradise Main Mix", "https://stream.radioparadise.com/aac-128"),
        New Station("Radio Paradise Mellow", "https://stream.radioparadise.com/mellow-128"),
        New Station("Radio Paradise Rock", "https://stream.radioparadise.com/rock-128"),
        New Station("Radio Paradise World/Etc", "https://stream.radioparadise.com/world-etc-128"),
        New Station("Radio Swiss Jazz", "https://stream.srg-ssr.ch/rsj/mp3_128.m3u"),
        New Station("Radio Swiss Classic", "https://stream.srg-ssr.ch/rsc_de/mp3_128.m3u"),
        New Station("Radio Swiss Pop", "https://stream.srg-ssr.ch/rsp/mp3_128.m3u"),
        New Station("Jazz Radio (France)", "https://jazzradio.ice.infomaniak.ch/jazzradio-high.mp3"),
        New Station("FIP (Radio France)", "https://icecast.radiofrance.fr/fip-midfi.mp3"),
        New Station("FIP Jazz", "https://icecast.radiofrance.fr/fipjazz-midfi.mp3"),
        New Station("FIP Groove", "https://icecast.radiofrance.fr/fipgroove-midfi.mp3"),
        New Station("FIP World", "https://icecast.radiofrance.fr/fipworld-midfi.mp3"),
        New Station("France Musique", "https://icecast.radiofrance.fr/francemusique-midfi.mp3"),
        New Station("Classic FM (Absolute)", "https://media-ice.musicradio.com/ClassicFMMP3"),
        New Station("Smooth Radio UK", "https://media-ice.musicradio.com/SmoothUKMP3"),
        New Station("listen.moe JP Anime", "https://listen.moe/stream"),
        New Station("listen.moe KR", "https://listen.moe/kpop/stream"),
        New Station("AnimeNfo Radio", "https://raimund.animenfo.com:443/;"),
        New Station("Frisky Radio", "https://stream.friskyradio.com/frisky_mp3_hi"),
        New Station("Frisky Deep", "https://stream.friskyradio.com/friskydeep_mp3_hi"),
        New Station("Frisky Chill", "https://stream.friskyradio.com/friskychill_mp3_hi"),
        New Station("Ambient Sleeping Pill", "https://radio.stereoscenic.com/asp-h"),
        New Station("Deep Ambient (Stereoscenic)", "https://radio.stereoscenic.com/dea-h"),
        New Station("BBC World Service", "https://stream.live.vc.bbcmedia.co.uk/bbc_world_service"),
        New Station("NPR News Now", "https://npr-ice.streamguys1.com/live.mp3"),
        New Station("WNYC FM", "https://fm939.wnyc.org/wnycfm"),
        New Station("KEXP 90.3", "https://kexp-mp3-128.streamguys1.com/kexp128.mp3"),
        New Station("WXPN", "https://wxpnhi.xpn.org/xpnhi"),
        New Station("The Current (MN Public)", "https://current.stream.publicradio.org/current.mp3"),
        New Station("Classical MPR", "https://cms.stream.publicradio.org/cms.mp3"),
        New Station("Radio Paradise Flac-ish AAC", "https://stream.radioparadise.com/aac-320")
    }
End Module
