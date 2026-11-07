# Recorder settings backup

Unity Recorder keeps its recorder list (the "Movie" recorder: Game View, 1024 x 1024, audio on, output to
`Recordings/<Recorder>_<Take>.mp4`) in `Library/Recorder/recorder.pref`, and `Library/` is not in git. If the Recorder
window comes up empty (after a fresh clone, a move, or deleting `Library/`), close Unity, copy `recorder.pref` from
this folder to `Library/Recorder/recorder.pref`, and reopen the project.
