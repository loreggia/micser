import type { Translations } from "@micser/web-sdk";
import type { en } from "./en";

export const de: Translations<typeof en> = {
  modules: {
    deviceInput: "Eingabegerät",
    loopbackInput: "Loopback",
    deviceOutput: "Ausgabegerät",
    gain: "Verstärkung",
    compressor: "Kompressor",
    noiseGate: "Noise Gate",
    ducker: "Ducker",
    delay: "Verzögerung",
    stereoTools: "Stereo-Werkzeuge",
    limiter: "Limiter",
    equalizer: "Equalizer",
    filter: "Filter",
    pitch: "Tonhöhe",
    voiceChanger: "Stimmveränderer",
    spectrum: "Spektrum",
  },
  descriptions: {
    deviceInput:
      "Nimmt ein Mikrofon, einen Line-Eingang oder ein anderes Aufnahmegerät auf. Verbinde seinen Ausgang mit den Modulen, die den Ton bearbeiten.",
    loopbackInput:
      "Nimmt auf, was ein Ausgabegerät abspielt, z. B. ein Spiel oder einen Browser, um es zu bearbeiten oder weiterzuleiten.",
    deviceOutput:
      "Spielt den verbundenen Ton auf Lautsprechern, Kopfhörern oder einem virtuellen Kabel ab. Mehrere Verbindungen werden gemischt.",
    gain: "Macht den Ton um einen festen Betrag lauter oder leiser.",
    compressor:
      "Gleicht die Lautstärke aus: Laute Stellen werden leiser (oder leise lauter), sodass z. B. eine Stimme gleichmäßig laut bleibt.",
    noiseGate:
      "Senkt den Ton ab, solange er leise ist, z. B. um Tastatur- und Raumgeräusche zwischen Wörtern zu entfernen.",
    ducker: "Senkt seinen Eingang ab, solange der Ton am Sidechain-Eingang laut ist, z. B. Musik, während du sprichst.",
    delay: "Verzögert den Ton kurz, z. B. um ihn mit einem Video oder einem anderen Tonweg zu synchronisieren.",
    stereoTools:
      "Bearbeitet den linken und rechten Kanal: tauschen oder zusammenführen, Stereobreite und Balance ändern oder einen umgepolten Kanal korrigieren.",
    limiter:
      "Hält Spitzen unter einer Obergrenze, sodass der Ton nie übersteuert oder zu laut wird. Oft das letzte Modul vor einer Ausgabe.",
    equalizer:
      "Ändert den Klang: hebt Bässe, Mitten oder Höhen an oder senkt sie ab, z. B. für eine klarere Stimme oder zur Kopfhörerkorrektur.",
    filter: "Entfernt die Frequenzen unter oder über einer Grenzfrequenz, z. B. tiefes Rumpeln eines Mikrofons.",
    pitch:
      "Verschiebt die Tonhöhe nach oben oder unten, ohne das Tempo zu ändern, z. B. für eine tiefere oder höhere Stimme.",
    voiceChanger:
      "Verändert eine Stimme, z. B. von männlich zu weiblich, in eine Kinder- oder Monsterstimme: verschiebt Tonhöhe und Klangcharakter (Formanten) getrennt.",
    spectrum:
      "Zeigt, welche Frequenzen der Ton enthält, von Bässen links bis zu Höhen rechts. Der Ton bleibt unverändert.",
  },
  device: {
    none: "Kein Gerät",
    unavailable: "Nicht verfügbar",
    unavailableAdapter: "Nicht verfügbar ({{adapter}})",
    deviceUnavailable: "{{name}} (nicht verfügbar)",
    notRunning: "Läuft nicht",
    running: "Läuft",
    dropouts_one: "{{count}} Aussetzer",
    dropouts_other: "{{count}} Aussetzer",
    buffer: "{{status}} · {{milliseconds}} ms Puffer",
    device: "Gerät",
    info: {
      DeviceInput: "Das Mikrofon oder andere Aufnahmegerät, das aufgenommen wird.",
      LoopbackInput: "Das Ausgabegerät, dessen Ton aufgenommen wird.",
      DeviceOutput: "Die Lautsprecher, Kopfhörer oder das virtuelle Kabel, auf denen der Ton abgespielt wird.",
    },
  },
  compressor: {
    downward: "Abwärts",
    upward: "Aufwärts",
    threshold: "Schwelle",
    ratio: "Verhältnis",
    attack: "Attack",
    release: "Release",
    knee: "Knie",
    makeUpGain: "Make-up-Gain",
    amount: "Anteil",
    type: "Typ",
    info: {
      type: "Abwärts macht die Stellen über der Schwelle leiser, die übliche Wahl. Aufwärts macht die Stellen darunter lauter, z. B. um leise Sprache anzuheben.",
      threshold:
        "Der Pegel, ab dem komprimiert wird: Abwärts ändert nur die Stellen, die lauter sind, aufwärts nur die, die leiser sind.",
      ratio:
        "Wie stark Pegel jenseits der Schwelle verändert werden. Bei 4:1 wird aus 4 dB jenseits der Schwelle 1 dB. 1:1 ändert nichts.",
      attack:
        "Wie schnell der Kompressor reagiert, wenn der Pegel die Schwelle überschreitet. Kurze Zeiten fangen schnelle Spitzen ab, längere erhalten den Anschlag von Klängen.",
      release:
        "Wie schnell der Kompressor wieder loslässt. Kurze Zeiten können die Lautstärke hörbar pumpen lassen, lange klingen weicher.",
      knee: "Macht den Übergang an der Schwelle weicher: Die Kompression setzt über so viele dB allmählich ein statt abrupt, was natürlicher klingt.",
      makeUpGain: "Hebt oder senkt den Pegel nach der Kompression, z. B. um die verlorene Lautstärke auszugleichen.",
      amount:
        "Mischt den komprimierten mit dem ursprünglichen Ton. 100 % ist voll komprimiert; kleinere Werte erhalten einen Teil der ursprünglichen Dynamik.",
    },
  },
  noiseGate: {
    threshold: "Schwelle",
    range: "Absenkung",
    attack: "Attack",
    hold: "Hold",
    release: "Release",
    hysteresis: "Hysterese",
    info: {
      threshold:
        "Das Gate öffnet, wenn der Ton lauter ist. Stelle es über das Hintergrundrauschen und unter deine leiseste Sprache.",
      range:
        "Wie stark der Ton abgesenkt wird, solange das Gate geschlossen ist. -100 dB ist praktisch still; z. B. -20 dB verringert nur das Rauschen, was natürlicher klingt.",
      attack:
        "Wie schnell das Gate öffnet, wenn der Ton laut wird. Sehr kurze Zeiten können klicken, lange schneiden Wortanfänge ab.",
      hold: "Wie lange das Gate offen bleibt, nachdem der Ton leise wurde, damit es in kurzen Pausen zwischen Wörtern nicht schließt.",
      release: "Wie schnell das Gate nach der Hold-Zeit schließt. Längere Zeiten lassen Wörter natürlich ausklingen.",
      hysteresis:
        "Das Gate schließt erst so viele dB unter der Schwelle, damit es nicht flackert, wenn der Pegel um die Schwelle schwankt.",
    },
  },
  ducker: {
    threshold: "Schwelle",
    depth: "Absenkung",
    attack: "Attack",
    hold: "Hold",
    release: "Release",
    info: {
      threshold: "Wenn der Ton am Sidechain-Eingang lauter ist, wird der Eingang abgesenkt.",
      depth: "Wie stark der Eingang abgesenkt wird, solange die Sidechain über der Schwelle ist. -100 dB ist still.",
      attack: "Wie schnell der Eingang leiser wird, wenn die Sidechain laut wird.",
      hold: "Wie lange der Eingang abgesenkt bleibt, nachdem die Sidechain leise wurde, damit er in kurzen Pausen nicht zurückkommt.",
      release: "Wie schnell der Eingang nach der Hold-Zeit wieder auf seinen Pegel kommt.",
    },
  },
  delay: {
    time: "Verzögerung",
    perChannel: "Pro Kanal",
    channel: "Kanal {{number}}",
    info: {
      time: "Wie lange der Ton zurückgehalten wird, z. B. um ihn mit einem Video oder einem Tonweg mit mehr Latenz abzugleichen.",
      perChannel:
        "Fügt jedem Kanal eine eigene zusätzliche Verzögerung hinzu, z. B. um unterschiedlich weit entfernte Lautsprecher abzugleichen.",
      channel: "Zusätzliche Verzögerung dieses Kanals, zur Verzögerung oben addiert.",
    },
  },
  stereoTools: {
    mode: "Modus",
    modes: {
      Stereo: "Stereo",
      Swap: "Links und rechts tauschen",
      LeftToBoth: "Links auf beiden Seiten",
      RightToBoth: "Rechts auf beiden Seiten",
      Mono: "Mono (Mittelwert)",
    },
    width: "Breite",
    balance: "Balance",
    center: "Mitte",
    left: "Links {{value}}",
    right: "Rechts {{value}}",
    invertLeft: "Polarität links umkehren",
    invertRight: "Polarität rechts umkehren",
    info: {
      mode: "Welcher Eingangskanal auf welche Seite geht: tauschen, eine Seite auf beiden abspielen oder beide zu Mono mischen, z. B. für ein Mikrofon, das nur auf einer Seite aufnimmt.",
      width:
        "0 % ist Mono, 100 % lässt das Stereobild unverändert, und höhere Werte machen es breiter, indem sie die Unterschiede zwischen links und rechts verstärken.",
      balance: "Verschiebt den Klang nach links oder rechts, indem die andere Seite leiser wird.",
      invertLeft:
        "Dreht die Wellenform des linken Kanals um. Korrigiert einen verkehrt verdrahteten Kanal, der mit dem anderen gemischt dünn oder hohl klingt.",
      invertRight:
        "Dreht die Wellenform des rechten Kanals um. Korrigiert einen verkehrt verdrahteten Kanal, der mit dem anderen gemischt dünn oder hohl klingt.",
    },
  },
  limiter: {
    ceiling: "Obergrenze",
    gain: "Verstärkung",
    release: "Release",
    latency: "Verzögert um {{value}}",
    info: {
      ceiling:
        "Der höchste Pegel der Ausgabe; nichts kommt darüber, sodass der Ton nicht übersteuern kann. -1 dB lässt etwas Spielraum.",
      gain: "Hebt den Pegel vor dem Limiter an, sodass der Ton lauter wird, während seine Spitzen unter der Obergrenze bleiben. Zu viel klingt gequetscht.",
      release:
        "Wie schnell der Pegel nach einer Spitze zurückkommt. Kurze Zeiten halten den Ton laut, können aber verzerren, längere klingen weicher.",
    },
  },
  equalizer: {
    band: "Band {{number}}",
    removeBand: "Band {{number}} entfernen",
    frequency: "Frequenz",
    gain: "Verstärkung",
    q: "Güte",
    addBand: "Band hinzufügen",
    response: "Frequenzgang",
    preamp: "Vorverstärkung",
    type: "Typ von Band {{number}}",
    types: {
      Peaking: "Glocke",
      LowShelf: "Low-Shelf",
      HighShelf: "High-Shelf",
      LowPass: "Tiefpass",
      HighPass: "Hochpass",
    },
    import: "Importieren…",
    importHint:
      "Ersetzt die Bänder und die Vorverstärkung durch die Filter einer Equalizer-APO-Konfiguration, z. B. eine Kopfhörerkorrektur von AutoEQ (ParametricEQ.txt).",
    imported_one: "{{count}} Band importiert.",
    imported_other: "{{count}} Bänder importiert.",
    importedSkipped: "{{imported}} Ausgelassen (andere Filtertypen oder einzelne Kanäle): {{skipped}}",
    importFailed: "Die Datei enthält keine Filter im Equalizer-APO-Format.",
    info: {
      preamp:
        "Ändert den Pegel vor den Bändern. Senke ihn etwa um deine größte Anhebung ab, damit der Ton nicht übersteuert.",
      type: "Glocke hebt um die Frequenz an oder senkt ab. Low- und High-Shelf heben alles darunter oder darüber an oder senken es ab. Tief- und Hochpass entfernen alles darüber oder darunter.",
      frequency:
        "Wo das Band wirkt: die Mitte einer Glocke, die Eckfrequenz der anderen. Niedrige Werte sind Bässe, um 1 kHz Mitten, hohe Werte Höhen.",
      gain: "Wie stark das Band den Ton anhebt (positiv) oder absenkt (negativ).",
      q: "Wie schmal das Band ist: Niedrige Werte ändern einen breiten Frequenzbereich sanft, hohe einen schmalen deutlich.",
    },
  },
  filter: {
    highPass: "Hochpass",
    lowPass: "Tiefpass",
    frequency: "Frequenz",
    slope: "Flankensteilheit",
    q: "Güte",
    decibelsPerOctave: "{{value}} dB/Okt.",
    response: "Frequenzgang",
    type: "Typ",
    info: {
      type: "Hochpass behält die Frequenzen über der Grenzfrequenz und entfernt die tiefen, z. B. Rumpeln und Brummen. Tiefpass behält die darunter, z. B. gegen Rauschen.",
      frequency:
        "Die Grenzfrequenz: Frequenzen jenseits davon werden abgesenkt. Für eine Stimme entfernt ein Hochpass bei etwa 80 Hz Rumpeln, ohne sie dünn zu machen.",
      slope:
        "Wie steil die Frequenzen jenseits der Grenzfrequenz abgesenkt werden. Höhere Werte schneiden schärfer ab.",
      q: "Die Resonanz an der Grenzfrequenz. 0,71 ist flach; höhere Werte erzeugen eine Spitze kurz vor der Grenzfrequenz, niedrigere einen sanfteren Übergang.",
    },
  },
  gain: {
    gain: "Verstärkung",
    info: {
      gain: "Wie viel lauter (positiv) oder leiser (negativ) der Ton wird. 0 dB lässt ihn unverändert; je 6 dB verdoppeln oder halbieren den Pegel.",
    },
  },
  pitch: {
    pitch: "Tonhöhe",
    semitones: "{{value}} HT",
    quality: "Qualität",
    info: {
      pitch:
        "Wie weit die Tonhöhe verschoben wird, in Halbtönen; 12 Halbtöne sind eine Oktave. Das Tempo bleibt gleich.",
      quality: "Höhere Werte klingen sauberer, erhöhen aber die Latenz und brauchen mehr CPU.",
    },
  },
  voiceChanger: {
    preset: "Voreinstellung",
    pitch: "Tonhöhe",
    formant: "Formant",
    mix: "Mischung",
    quality: "Qualität",
    breathiness: "Atem",
    wobble: "Zittern",
    effect: "Effekt",
    mode: "Tonhöhenmodus",
    targetPitch: "Ziel",
    semitones: "{{value}} HT",
    presets: {
      Custom: "Benutzerdefiniert",
      MaleToFemale: "Männlich zu weiblich",
      FemaleToMale: "Weiblich zu männlich",
      Child: "Kind",
      Old: "Ältere Person",
      Monster: "Monster",
      Chipmunk: "Streifenhörnchen",
      Robot: "Roboter",
      Whisper: "Flüstern",
      Alien: "Außerirdischer",
      Radio: "Funk",
    },
    effects: {
      None: "Kein Effekt",
      Robot: "Roboter",
      Whisper: "Flüstern",
      Alien: "Außerirdischer",
      Radio: "Funk",
      Monotone: "Monoton",
      NoteSnap: "Auf Töne einrasten",
    },
    modes: {
      Shift: "Um Halbtöne verschieben",
      Target: "Zieltonhöhe",
    },
    info: {
      preset:
        "Ein Ausgangspunkt für Tonhöhe, Formant, Atem, Zittern und Effekt. Wird einer davon danach geändert, ist es eine benutzerdefinierte Einstellung.",
      effect:
        "Roboter spricht auf einem einzigen Ton (mit der Tonhöhe verschiebbar), Flüstern ersetzt die Stimme durch Atem, Außerirdischer fügt einen metallischen Klang hinzu, Funk klingt wie ein Telefon oder Walkie-Talkie, Monoton macht deine Sprachmelodie flach und Auf Töne einrasten zieht die Tonhöhe auf den nächsten Ton, wie bei hart gestimmtem Gesang.",
      mode: "Verschieben ändert die Tonhöhe um eine feste Zahl von Halbtönen. Zieltonhöhe bringt deine durchschnittliche Tonhöhe auf eine Frequenz, sodass z. B. eine Frauenstimme gleich hoch herauskommt, egal ob deine eigene Stimme tief oder hoch ist. Sie folgt deiner durchschnittlichen Tonhöhe, nimm zum Singen daher Verschieben.",
      targetPitch:
        "Die Tonhöhe, die deine Stimme im Durchschnitt haben soll. Typische Sprechstimmen liegen bei etwa 120 Hz für Männer, 210 Hz für Frauen und 300 Hz für Kinder. Deine Sprachmelodie bleibt erhalten.",
      breathiness:
        "Ersetzt einen Teil der Stimme durch Atemgeräusch, z. B. für eine hauchige, luftige oder ältere Stimme.",
      wobble: "Lässt die Stimme leicht in Tonhöhe und Lautstärke zittern, wie bei einer älteren oder nervösen Stimme.",
      pitch:
        "Wie weit die Tonhöhe verschoben wird, in Halbtönen; 12 Halbtöne sind eine Oktave. Der Klangcharakter der Stimme bleibt gleich.",
      formant:
        "Verschiebt den Klangcharakter der Stimme (ihre Formanten), in Halbtönen: nach oben klingt sie kleiner, wie eine Frau oder ein Kind; nach unten größer, wie ein Mann oder ein Riese.",
      mix: "Wie viel von der veränderten Stimme zu hören ist; der Rest ist die Originalstimme.",
      quality: "Höhere Werte klingen sauberer, erhöhen aber die Latenz und brauchen mehr CPU.",
    },
  },
};
