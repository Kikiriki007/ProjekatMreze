# Space Invaders - Multiplayer

Multiplayer Space Invaders igra razvijena u C# sa TCP/UDP mrežnom komunikacijom kao deo projekta iz predmeta Računarske Mreže.

## Opis Projekta

Klasična Space Invaders igra prilagođena za multiplayer. Projekat demonstrira implementaciju klijent-server arhitekture sa TCP i UDP protokolima za sinhronizaciju stanja igre između dva konkurentna igrača.

## Mrežna Arhitektura
<img width="1000" height="900" alt="svgviewer-png-output(1)" src="https://github.com/user-attachments/assets/c2624ba6-eee7-4bae-b3c1-81b29e2599a7" />

### Protokoli

**TCP (Port 5000)**
- Koristi se za inicijalni handshake i autentifikaciju
- Client šalje LoginRequest sa imenom igrača
- Server dodeljuje PlayerNumber (1 ili 2) i PlayerType (BULLETPLAYER ili BROADSIDEPLAYER)
- Server šalje LoginResponse sa dodeljenim podacima
- Konekcija se zatvara nakon uspešnog login-a

**UDP (Port 5001)**
- Koristi se za real-time komunikaciju tokom igre
- Client → Server: InputPacket (komande igrača, 60 puta u sekundi)
- Server → Clients: GameState (kompletno stanje igre, 60 FPS broadcast)
- Nema garantovane isporuke - igra tolerise gubitak paketa

### Komunikacioni Tok
<img width="1000" height="800" alt="svgviewer-png-output" src="https://github.com/user-attachments/assets/d5680995-0b27-4657-b910-07931acc903b" />
