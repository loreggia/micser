export const en = {
  modules: {
    deviceInput: "Input device",
    loopbackInput: "Loopback",
    deviceOutput: "Output device",
    gain: "Gain",
    compressor: "Compressor",
    noiseGate: "Noise gate",
    ducker: "Ducker",
    delay: "Delay",
    stereoTools: "Stereo tools",
    limiter: "Limiter",
    equalizer: "Equalizer",
    filter: "Filter",
    pitch: "Pitch",
    spectrum: "Spectrum",
  },
  descriptions: {
    deviceInput:
      "Records a microphone, line input or other recording device. Connect its output to the modules that process the audio.",
    loopbackInput:
      "Captures what an output device plays, e.g. a game or a browser, so you can process it or pass it on.",
    deviceOutput:
      "Plays the audio connected to it on speakers, headphones or a virtual cable. Several connections are mixed together.",
    gain: "Makes the audio louder or quieter by a fixed amount.",
    compressor:
      "Evens out the volume: loud parts get quieter (or quiet parts louder), so e.g. a voice stays at a steady level.",
    noiseGate: "Lowers the audio while it's quiet, e.g. to remove keyboard and room noise between words.",
    ducker: "Lowers its input while the audio on its Sidechain input is loud, e.g. music while you speak.",
    delay: "Holds the audio back for a moment, e.g. to bring it in sync with a video or with another audio path.",
    stereoTools:
      "Adjusts the left and right channels: swap or combine them, change the stereo width and balance, or fix an inverted channel.",
    limiter:
      "Keeps peaks below a ceiling, so the audio never clips or gets too loud. Often the last module before an output.",
    equalizer:
      "Changes the tone: boosts or cuts bass, mids or treble, e.g. to make a voice clearer or to correct headphones.",
    filter: "Removes the frequencies below or above a cutoff, e.g. low rumble from a microphone.",
    pitch: "Shifts the pitch up or down without changing the speed, e.g. to make a voice deeper or higher.",
    spectrum:
      "Shows which frequencies the audio contains, from bass on the left to treble on the right. The audio passes unchanged.",
  },
  device: {
    none: "No device",
    unavailable: "Unavailable",
    unavailableAdapter: "Unavailable ({{adapter}})",
    deviceUnavailable: "{{name}} (unavailable)",
    notRunning: "Not running",
    running: "Running",
    dropouts_one: "{{count}} dropout",
    dropouts_other: "{{count}} dropouts",
    buffer: "{{status}} · {{milliseconds}} ms buffer",
    device: "Device",
    info: {
      DeviceInput: "The microphone or other recording device to capture.",
      LoopbackInput: "The output device whose sound is captured.",
      DeviceOutput: "The speakers, headphones or virtual cable to play the audio on.",
    },
  },
  compressor: {
    downward: "Downward",
    upward: "Upward",
    threshold: "Threshold",
    ratio: "Ratio",
    attack: "Attack",
    release: "Release",
    knee: "Knee",
    makeUpGain: "Make-up gain",
    amount: "Amount",
    type: "Type",
    info: {
      type: "Downward makes the parts above the threshold quieter, the usual choice. Upward makes the parts below it louder, e.g. to bring up quiet speech.",
      threshold:
        "The level where compression starts: downward compression only changes the parts louder than this, upward compression the parts quieter than this.",
      ratio:
        "How strongly levels beyond the threshold are changed. At 4:1, a level 4 dB beyond the threshold comes out 1 dB beyond it. 1:1 changes nothing.",
      attack:
        "How quickly the compressor reacts when the level crosses the threshold. Short times catch fast peaks, longer ones keep the start of sounds punchy.",
      release:
        "How quickly the compressor lets go when the level is back. Short times can make the volume audibly pump, long ones sound smoother.",
      knee: "Softens the change at the threshold: the compression fades in over this many dB instead of starting abruptly, which sounds more natural.",
      makeUpGain: "Raises or lowers the level after the compression, e.g. to make up for the volume it took away.",
      amount:
        "Mixes the compressed with the original audio. 100 % is fully compressed; lower values keep some of the original dynamics.",
    },
  },
  noiseGate: {
    threshold: "Threshold",
    range: "Range",
    attack: "Attack",
    hold: "Hold",
    release: "Release",
    hysteresis: "Hysteresis",
    info: {
      threshold:
        "The gate opens when the audio is louder than this. Set it above the background noise and below your quietest speech.",
      range:
        "How much the audio is lowered while the gate is closed. -100 dB is practically silent; e.g. -20 dB only reduces the noise, which sounds more natural.",
      attack:
        "How quickly the gate opens when the audio gets loud. Very short times can click, long ones cut off the start of words.",
      hold: "How long the gate stays open after the audio got quiet, so it doesn't close in short pauses between words.",
      release: "How quickly the gate closes after the hold time. Longer times let words fade out naturally.",
      hysteresis:
        "The gate closes only this many dB below the threshold, so it doesn't flicker when the level hovers around the threshold.",
    },
  },
  ducker: {
    threshold: "Threshold",
    depth: "Depth",
    attack: "Attack",
    hold: "Hold",
    release: "Release",
    info: {
      threshold: "When the audio on the Sidechain input is louder than this, the input is lowered.",
      depth: "How much the input is lowered while the Sidechain is above the threshold. -100 dB is silent.",
      attack: "How quickly the input goes down when the Sidechain gets loud.",
      hold: "How long the input stays down after the Sidechain got quiet, so it doesn't come back in short pauses.",
      release: "How quickly the input comes back to its level after the hold time.",
    },
  },
  delay: {
    time: "Delay",
    perChannel: "Per channel",
    channel: "Channel {{number}}",
    info: {
      time: "How long the audio is held back, e.g. to line it up with a video or with another path that has more latency.",
      perChannel: "Adds an extra delay for each channel, e.g. to align speakers at different distances.",
      channel: "Extra delay of this channel, added to the delay above.",
    },
  },
  stereoTools: {
    mode: "Mode",
    modes: {
      Stereo: "Stereo",
      Swap: "Swap left and right",
      LeftToBoth: "Left on both sides",
      RightToBoth: "Right on both sides",
      Mono: "Mono (average)",
    },
    width: "Width",
    balance: "Balance",
    center: "Center",
    left: "Left {{value}}",
    right: "Right {{value}}",
    invertLeft: "Invert left polarity",
    invertRight: "Invert right polarity",
    info: {
      mode: "Which input channel goes to which side: swap them, play one side on both, or mix both to mono, e.g. for a microphone that only records on one side.",
      width:
        "0 % is mono, 100 % leaves the stereo image unchanged, and higher values make it wider by boosting the differences between left and right.",
      balance: "Moves the sound toward the left or the right by lowering the other side.",
      invertLeft:
        "Flips the waveform of the left channel. Fixes a channel that's wired in reverse, which sounds thin or hollow when mixed with the other one.",
      invertRight:
        "Flips the waveform of the right channel. Fixes a channel that's wired in reverse, which sounds thin or hollow when mixed with the other one.",
    },
  },
  limiter: {
    ceiling: "Ceiling",
    gain: "Gain",
    release: "Release",
    latency: "Adds {{value}} latency",
    info: {
      ceiling:
        "The highest level the output reaches; nothing gets past it, so the audio can't clip. -1 dB leaves a little margin.",
      gain: "Raises the level before limiting, so the audio gets louder while its peaks stay below the ceiling. Too much sounds squashed.",
      release:
        "How quickly the level comes back after a peak. Short times keep the audio loud but can distort, longer ones sound smoother.",
    },
  },
  equalizer: {
    band: "Band {{number}}",
    removeBand: "Remove band {{number}}",
    frequency: "Frequency",
    gain: "Gain",
    q: "Q",
    addBand: "Add band",
    response: "Frequency response",
    preamp: "Preamp",
    type: "Type of band {{number}}",
    types: {
      Peaking: "Peaking",
      LowShelf: "Low shelf",
      HighShelf: "High shelf",
      LowPass: "Low-pass",
      HighPass: "High-pass",
    },
    import: "Import…",
    importHint:
      "Replaces the bands and the preamp with the filters of an Equalizer APO configuration, e.g. a headphone correction from AutoEQ (ParametricEQ.txt).",
    imported_one: "Imported {{count}} band.",
    imported_other: "Imported {{count}} bands.",
    importedSkipped: "{{imported}} Left out (other filter types or single channels): {{skipped}}",
    importFailed: "The file has no filters in the Equalizer APO format.",
    info: {
      preamp:
        "Changes the level before the bands. Lower it by about as much as your largest boost, so the audio doesn't clip.",
      type: "Peaking boosts or cuts around the frequency. Low and high shelves boost or cut everything below or above it. Low-pass and high-pass remove everything above or below it.",
      frequency:
        "Where the band works: the center of a peaking band, the corner of the others. Low values are bass, around 1 kHz are mids, high values treble.",
      gain: "How much the band boosts (positive) or cuts (negative) the audio.",
      q: "How narrow the band is: low values change a wide range of frequencies gently, high values a narrow range sharply.",
    },
  },
  filter: {
    highPass: "High-pass",
    lowPass: "Low-pass",
    frequency: "Frequency",
    slope: "Slope",
    q: "Q",
    decibelsPerOctave: "{{value}} dB/oct",
    response: "Frequency response",
    type: "Type",
    info: {
      type: "High-pass keeps the frequencies above the cutoff and removes the low ones, e.g. rumble and hum. Low-pass keeps the ones below, e.g. against hiss.",
      frequency:
        "The cutoff: frequencies beyond it are reduced. For a voice, a high-pass at about 80 Hz removes rumble without making it thin.",
      slope: "How steeply the frequencies beyond the cutoff are reduced. Higher values cut more sharply.",
      q: "The resonance at the cutoff. 0.71 is flat; higher values add a peak just before the cutoff, lower ones a gentler bend.",
    },
  },
  gain: {
    gain: "Gain",
    info: {
      gain: "How much louder (positive) or quieter (negative) the audio gets. 0 dB leaves it unchanged; every 6 dB double or halve the level.",
    },
  },
  pitch: {
    pitch: "Pitch",
    semitones: "{{value}} st",
    quality: "Quality",
    info: {
      pitch: "How far the pitch is shifted, in semitones; 12 semitones are an octave. The speed stays the same.",
      quality: "Higher values sound cleaner, but add latency and need more CPU.",
    },
  },
};
