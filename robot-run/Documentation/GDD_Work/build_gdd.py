from pathlib import Path
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT=Path(__file__).resolve().parent
OUT=ROOT.parent/'GDD_Core_Mechanics_v0.4.docx'
d=Document()
s=d.sections[0]
s.page_height=Cm(29.7); s.page_width=Cm(21)
s.top_margin=Cm(1.7); s.bottom_margin=Cm(1.7); s.left_margin=Cm(2); s.right_margin=Cm(2)
for name in ['Normal','Title','Subtitle','Heading 1','Heading 2','Heading 3']:
 st=d.styles[name]; st.font.name='Calibri'; st.font.color.rgb=RGBColor(0,0,0)
d.styles['Normal'].font.size=Pt(10)
d.styles['Normal'].paragraph_format.space_after=Pt(6)
d.styles['Normal'].paragraph_format.line_spacing=1.08
d.styles['Title'].font.size=Pt(25)
d.styles['Heading 1'].font.size=Pt(18)
d.styles['Heading 2'].font.size=Pt(12)
def p(t): d.add_paragraph(t)
def h(t): d.add_heading(t,2)
def page(n,title):
 if n>1:d.add_page_break()
 d.add_heading(f'{n:02d}  {title}',1)
def table(headers,rows):
 t=d.add_table(rows=1,cols=len(headers));t.style='Light Shading Accent 1'
 for c,x in zip(t.rows[0].cells,headers): c.text=x
 for row in rows:
  for c,x in zip(t.add_row().cells,row):c.text=x
 for row in t.rows:
  for c in row.cells:
   for pp in c.paragraphs:
    pp.paragraph_format.space_after=Pt(4)
    for r in pp.runs:r.font.size=Pt(9)
 for c in t.rows[0].cells:
  tcPr=c._tc.get_or_add_tcPr(); sh=OxmlElement('w:shd');sh.set(qn('w:fill'),'E8EDF1');tcPr.append(sh)
  for r in c.paragraphs[0].runs:r.font.color.rgb=RGBColor(0,0,0);r.bold=True
 for row in t.rows:
  pr=row._tr.get_or_add_trPr();pr.append(OxmlElement('w:cantSplit'))

page(1,'Visione e contratto di gioco')
d.add_paragraph('GDD tecnico delle meccaniche', 'Title')
p('Robot Run | Versione 0.4 | 6 novembre 2026')
p('Puzzle platform 2D a programmazione tramite carte. Il giocatore conduce uno o più robot fuori da una fabbrica, preparando una sequenza limitata di istruzioni e osservandone l’esecuzione autonoma.')
h('Obiettivo della specifica')
p('Definire movimento, cicli, oggetti ambientali, segnali, interfaccia e feedback con regole sufficientemente precise per progettazione dei livelli e implementazione. La simulazione usa una griglia discreta; l’animazione visualizza gli eventi già risolti dalla logica.')
h('Stato delle decisioni')
p('CONFERMATO identifica regole presenti nella v0.3 o approvate nella discussione successiva. PROPOSTA identifica una soluzione tecnica di questa revisione da validare prima di considerarla definitiva. APERTO identifica una scelta ancora priva di specifica esecutiva. I parametri proposti non sono bilanciamento definitivo.')
p('Questa revisione conserva le regole fondamentali della v0.3 e amplia oggetti, timer e feedback. Le proposte precedentemente discusse ma non approvate esplicitamente restano proposte. Non costituisce una descrizione delle funzionalità già implementate nel progetto Unity.')
h('Vincoli confermati')
p('Stessa configurazione iniziale e stesso programma producono lo stesso risultato. Stanza interamente visibile, telecamera ortogonale laterale, celle logiche 1×1 e asse Y positivo verso l’alto. Nessuna correzione manuale durante Play. Carte disponibili e numero massimo di istruzioni sono proprietà del livello; il mazzo non è casuale.')
h('Struttura della specifica')
p('02 Modello logico e cicli · 03 Carte e movimento · 04 Pipeline degli eventi · 05 Nastri e portali · 06 Botole e trappole · 07 Interruttori e segnali · 08 Oggetti e timer · 09 Programmazione e input · 10 Esecuzione e feedback · 11 Dati e robot multipli · 12 Casi di accettazione e decisioni aperte.')
h('Ciclo del giocatore')
p('Osserva geometria, stati e collegamenti → compone il programma → avvia → osserva il punto di successo o errore → torna alla programmazione con le carte conservate → modifica e riprova. Nessun elemento ambientale conserva modifiche tra tentativi, salvo future regole esplicite.')

page(2,'Modello logico e conteggio dei cicli')
h('Posizioni e occupazione')
p('CONFERMATO. Le interazioni dipendono da posizioni discrete e celle attraversate. PROPOSTA. Robot e casse standard occupano una cella; piattaforme e botole forniscono supporto sotto la cella occupata. Separare terreno, entità solide e trigger consente a un robot di stare su un nastro o una piastra senza considerarli ostacoli sovrapposti.')
h('Definizione di ciclo')
p('CONFERMATO. Una traslazione effettiva del personaggio di una cella lungo X genera un ciclo, a destra o a sinistra. Il conteggio non dipende da frame, secondi, velocità delle animazioni o numero di carte. Il trasporto sul nastro genera cicli; la caduta verticale no. Il teletrasporto non aggiunge cicli: conta già il passo della carta o del nastro che ha raggiunto il portale.')
table(['Evento','Cicli del robot','Residuo MOVE'],[
('Passo orizzontale della carta','+1','−1'),('Passo orizzontale del nastro','+1','Invariato'),('Componente orizzontale di JUMP X','+1','Non applicabile'),('Salita o caduta verticale','0','Invariato'),('Teletrasporto','0','Invariato'),('Tentativo bloccato prima dello spostamento','0','Non consumato')])
h('Stato minimo del robot')
p('PROPOSTA. robotId, cella, stato, indiceCarta, tipoComando, direzioneCarta, passiResidui, nastroAttuale e protezioneRientroPortale. Stati: in attesa, esegue carta, trasportato, in caduta, trasferimento, uscito, errore. L’uscita e l’errore sono terminali per il robot nel tentativo.')
h('Stato del tentativo')
p('PROPOSTA. Fasi: programmazione, esecuzione, successo, fallimento. Play cattura una copia immutabile del programma e ricostruisce la configurazione iniziale. Ogni evento logico riceve un indice progressivo per ricostruire la causa di un errore. I contatori dei timer sono interi non negativi.')
h('Tempo senza movimento')
p('CONFERMATO per i timer legati ai cicli. Un’animazione ferma o una caduta non consuma cicli. PROPOSTA. Un robot impossibilitato a procedere non attende che un timer scada da solo: nessun secondo reale può risolvere una situazione di stallo. Gli effetti senza spostamento devono comunque terminare in un numero finito di eventi.')

page(3,'Carte e risoluzione del movimento')
h('MOVE X')
p('CONFERMATO. La carta richiede X celle orizzontali nella propria direzione. Ogni cella è verificata separatamente; un movimento obbligatorio impossibile causa cortocircuito e fallimento. Il comando può essere sospeso dal nastro senza perdere direzione o passi residui. La carta successiva parte quando il comando corrente è interamente utilizzato e le conseguenze ambientali sono risolte.')
h('JUMP X')
p('CONFERMATO. Da (x,y), la destinazione nominale è (x+d,y+X), con d uguale a +1 o −1. JUMP X sale di X celle e avanza di una sola cella orizzontale. Urti contro soffitto o ostacolo laterale interrompono il salto e provocano ricaduta, senza cortocircuito; la carta viene consumata.')
p('PROPOSTA per chiudere il percorso intermedio. Verificare in sequenza (x,y+1)…(x,y+X), poi (x+d,y+X). L’arco grafico non autorizza attraversamenti ulteriori. Se una salita è bloccata, fermarsi all’ultima cella libera e risolvere la caduta. Se è bloccato il passo orizzontale, nessun ciclo viene consumato. Se quel passo riesce, genera un ciclo, qualunque sia X.')
h('Appoggio e gravità')
p('CONFERMATO. In assenza di appoggio il robot cade subito; il primo elemento incontrato determina atterraggio, morte o teletrasporto. PROPOSTA. Verificare l’appoggio dopo ogni passo orizzontale, anche durante MOVE X; sospendere il comando durante la caduta e riprenderlo all’atterraggio. Questo precisa la v0.3, che collocava la caduta dopo il comando, e richiede validazione.')
p('PROPOSTA. La caduta attraversa le celle verticali in ordine, senza cicli; un supporto arresta la discesa. Cadere oltre il limite inferiore del livello produce fallimento con causa «caduta fuori dalla stanza». Atterrare sopra una cassa solida è ammesso; non la spinge orizzontalmente.')
h('Completamento e casi limite')
p('CONFERMATO. L’uscita è raggiunta al contatto senza eseguire le carte residue. PROPOSTA. Un programma esaurito senza raggiungere l’uscita termina con «programma esaurito», senza esplosione. Con passi residui pari a zero, un nastro attivo o una caduta devono comunque finire prima dell’avvio della carta seguente.')
p('PROPOSTA. Valori delle carte interi strettamente positivi; nessuna carta WAIT, direzione verticale o rotazione in esecuzione nella versione base. Le collisioni con casse durante il salto non causano spinte: applicano la normale interruzione del salto.')

page(4,'Pipeline deterministica degli eventi')
p('PROPOSTA TECNICA. L’ordine seguente rende verificabili gli eventi simultanei e precisa la sequenza generale della v0.3. Non usare l’ordine dei callback fisici o la durata delle animazioni per decidere il risultato.')
table(['Fase','Operazione'],[
('1 Intenzione','Selezionare il prossimo passo della carta, del nastro o della caduta; acquisire i timer già attivi.'),
('2 Validazione','Verificare limiti, solidi e spinta. Se un passo obbligatorio è impossibile, emettere l’errore senza movimento né ciclo.'),
('3 Commit','Applicare atomicamente lo spostamento e l’eventuale spinta; decrementare il MOVE solo se il passo proviene dalla carta.'),
('4 Contatti','Registrare uscita/ingresso da celle e piastre; rilevare trappole e uscita. Un contatto letale prevale sul successo.'),
('5 Ciclo e timer','Se il robot si è mosso orizzontalmente, avanzare il ciclo e decrementare solo i timer attivi prima di questo passo.'),
('6 Segnali','Applicare pressioni, rilasci, attivazioni e scadenze. Un timer appena attivato o ricaricato conserva X cicli.'),
('7 Effetti','Risolvere esplosioni e cambiamenti di solidità; verificare morte prima di confermare un’uscita candidata.'),
('8 Ambiente','Risolvere portale, appoggio, caduta e trasporto come nuovi eventi ordinati, finché il robot può riprendere la carta.'),
('9 Avanzamento','Se il residuo è zero e nessun effetto trattiene il robot, passare alla carta successiva.')])
h('Priorità e conflitti')
p('PROPOSTA. Evitare nel level design la sovrapposizione di uscita, portale e trappola nella stessa cella. In un dato invalido, morte prima di uscita, uscita prima di portale. Gli effetti letali nella stessa transazione del contatto con l’uscita causano fallimento. Questa precisazione della vittoria al contatto richiede approvazione.')
p('Le modifiche alle piastre si calcolano sulla configurazione completa dopo la spinta: la cassa può premere una piastra mentre il robot libera un’altra. Aggregare i segnali prima di aggiornare i ricevitori, evitando attivazioni transitorie dovute all’ordine di scansione.')
h('Fine timer durante il trasporto')
p('CONFERMATO nel comportamento, PROPOSTA nell’ordine tecnico. Il passo del nastro che porta il contatore da 1 a 0 viene completato. Poi il nastro si spegne; prima del passo seguente si verifica l’appoggio e si riprende il MOVE sospeso, oppure si avanza carta se il residuo è zero.')

page(5,'Nastri trasportatori e portali')
h('Nastro trasportatore')
p('CONFERMATO. Entrare su un nastro acceso sospende MOVE X. Il trasporto non consuma i passi residui ma ogni cella orizzontale genera un ciclo. All’uscita dal nastro, o quando questo si spegne sotto il robot, il comando riprende nella direzione originale. Un ostacolo che impedisce il trasporto manda il sistema in crash. Circuiti senza uscita non devono essere presenti nei livelli.')
p('PROPOSTA. Il nastro spento è un supporto ordinario. Il nastro acceso agisce solo sul robot appoggiato: sorvolarlo non trasporta. Il robot atterrato da salto o caduta è preso in carico prima della carta successiva. La direzione del nastro è indipendente da quella della carta e non viene trasferita a essa.')
p('PROPOSTA. Ogni passo sposta di una cella, compreso il passo oltre l’ultima cella del nastro; nella nuova cella si verificano supporto, portale e altri nastri. Un nastro contiguo può prendere in carico il robot. Non si esegue contemporaneamente un passo MOVE e un passo nastro.')
h('Esempio di sospensione')
p('MOVE 5 verso destra: due passi portano sul nastro, residuo 3. Il nastro trasporta per due celle; il timer scade e lo spegne. Il robot esegue i tre passi residui verso destra. Il totale è sette cicli orizzontali, non cinque. Nessuna carta successiva è stata avviata durante il trasporto.')
h('Portale')
p('CONFERMATO. Il teletrasporto non genera cicli aggiuntivi. PROPOSTA. Coppie fisse bidirezionali con colore e simbolo comuni. L’ingresso attivo trasferisce alla cella del partner; MOVE conserva residuo e direzione. L’arrivo non riattiva il partner finché il robot non lascia la cella. Un portale intercettato in caduta trasferisce senza costo in cicli e poi rivaluta il supporto.')
p('PROPOSTA. La coppia si abilita come unità. Un portale spento è un trigger inattivo, non un pavimento né un muro. Se la destinazione è occupata da un solido, fallimento «destinazione portale occupata», senza sovrapposizione. Portali non modificano l’orientamento delle carte e non trasportano casse nella versione base.')
h('Leggibilità')
p('PROPOSTA. Nastri: frecce sempre visibili, animazione solo da accesi, stato testuale nell’ispezione. Portali: simbolo della coppia oltre al colore, evidenza del partner al passaggio del puntatore. Il feedback di trasferimento mostra entrambi gli estremi senza nascondere il contatore del MOVE.')

page(6,'Botole e trappole')
h('Botola a cedimento')
p('CONFERMATO. Esiste una botola che cede al passaggio. PROPOSTA di temporizzazione: il primo appoggio la arma; si apre quando il robot lascia la cella, oppure alla fine della carta se il robot rimane sopra. In questo secondo caso provoca la caduta prima della carta seguente. Rimane aperta fino al reset. Un salto che la sorvola senza appoggio non la arma.')
p('PROPOSTA. Stati: integra, armata, aperta. Il passaggio di una cassa appoggiata applica la stessa regola, ma la sua fine movimento corrisponde alla fine del passo di spinta: una cassa lasciata sopra causa l’apertura dopo quel passo. Questo comportamento delle casse richiede validazione separata.')
h('Botola comandata')
p('CONFERMATO. Il segnale di un pulsante può aprirla o chiuderla. Se è aperta il robot cade dentro; se si apre sotto il robot, viene meno l’appoggio. PROPOSTA. Configurazione per istanza «segnale attivo = aperta» oppure «segnale attivo = chiusa», visibile nell’ispezione.')
p('APERTO. Chiusura quando il vano è occupato. Cadere nella botola non definisce automaticamente questo caso. PROPOSTA tecnica da validare: chiusura pendente finché il volume è libero, segnalata da spia lampeggiante; il robot continua a cadere. Nessun respingimento o schiacciamento implicito.')
h('Trappola')
p('CONFERMATO. Cadere su una trappola uccide il robot. PROPOSTA. Una trappola attiva uccide a ogni contatto con la cella pericolosa, anche per MOVE, salto, nastro o arrivo da portale. Una trappola spenta non è letale; supporto e solidità sono proprietà esplicite della sua base. Prima versione: trappola fissa o comandata, senza alternanza autonoma in secondi.')
p('PROPOSTA. Se una trappola si attiva mentre il robot occupa la cella pericolosa, l’effetto letale è immediato nella fase effetti. Per le casse, la trappola blocca o distrugge solo se il suo profilo dichiara esplicitamente quella risposta; base proposta: danneggia i robot e lascia intatte le casse.')
h('Feedback di stato')
p('PROPOSTA. Cedimento: crepe e scatto sonoro all’armamento, apertura leggibile e suono distinto. Comandata: sagoma del vano aperto, pannello chiuso ben riconoscibile e indicatore del segnale. Trappola: denti o zona pericolosa leggibili anche da spenta; luce e movimento indicano l’attivazione. Non affidare mai il significato al solo colore.')

page(7,'Interruttori segnali e timer')
p('CONFERMATO. Esistono interruttori di vario genere che accendono e spengono gli accessori, inclusi pulsanti temporizzati. Il giocatore li aziona attraverso il movimento programmato del robot; PROPOSTA: nessuna attivazione diretta con il mouse durante Play.')
table(['Dispositivo','Comportamento proposto','Riattivazione'],[
('Piastra a pressione','Segnale attivo finché robot o cassa occupano la cella di attivazione.','Segue l’occupazione.'),
('Pulsante a scatto','Ogni nuovo ingresso alterna il segnale persistente.','Uscire e rientrare.'),
('Pulsante monouso','Il primo ingresso attiva il segnale fino al reset.','Nessuna nello stesso tentativo.'),
('Pulsante a timer','Il nuovo ingresso attiva il segnale per X cicli orizzontali del personaggio.','Nuovo ingresso ricarica X.')])
h('Contratto temporale')
p('CONFERMATO. I timer seguono celle percorse lungo X, comprese quelle del nastro; verticale e teletrasporto non consumano tempo. PROPOSTA. Il passo che preme il pulsante non consuma il timer appena avviato. A zero il segnale si disattiva nella stessa transazione, prima del prossimo movimento. X è intero ≥1; zero è configurazione invalida, non durata istantanea.')
p('PROPOSTA. Restare sulla superficie non genera pressioni ripetute. La pressione richiede ingresso o primo appoggio; un salto sopra il dispositivo non lo aziona. Una piastra occupata inizialmente ha già il segnale attivo nello stato iniziale, senza inventare un movimento del robot.')
h('Collegamenti')
p('PROPOSTA. Un interruttore può comandare più ricevitori. Ogni collegamento riporta ID sorgente, ID destinazione e polarità. Il ricevitore deriva lo stato da segnale e polarità, senza accumulare comandi toggle provenienti dai fili. Più sorgenti sullo stesso ricevitore sono aggregate in OR: basta una attiva. Vietare polarità contraddittorie sullo stesso ricevitore.')
p('PROPOSTA. I cicli di retroazione tra segnali non sono ammessi nella versione base. Nessun ricevitore emette a sua volta un segnale. La topologia e gli stati iniziali devono essere ispezionabili prima di Play.')
h('Feedback del timer')
p('PROPOSTA. Contatore numerico sul dispositivo e nell’ispezione, aggiornato soltanto agli eventi logici. Ogni decremento produce un breve impulso visivo; a 1 appare un avviso distinto, a 0 uno scatto di spegnimento. La ricarica mostra il ritorno a X. Nessuna barra in secondi o ticchettio continuo che suggerisca un tempo reale.')

page(8,'Oggetti solidi spostabili e temporizzati')
h('Categorie richieste')
p('CONFERMATO. Oggetti interagibili/spostabili si muovono al contatto orizzontale; oggetti non interagibili bloccano il passaggio orizzontale; oggetti interagibili a tempo possono essere spostati e poi esplodere, indurirsi o cambiare comportamento. Le regole operative seguenti sono PROPOSTE, inclusi innesco e conseguenze dei timer.')
h('Spinta e ostacoli')
p('Una spinta riuscita trasla di una cella la cassa e porta il robot nella cella liberata. Occorre che la destinazione della cassa sia libera e dentro i limiti. Il passo genera un solo ciclo, dovuto al robot: lo spostamento della cassa non lo raddoppia. Nessuna trazione e nessuna spinta verticale. Nella base non si spingono catene: una seconda cassa blocca la prima.')
p('Contro un oggetto immobile, o una cassa impossibile da spingere, il robot resta nella cella precedente e il tentativo fallisce per movimento obbligatorio bloccato. «Rimani bloccato» è qui interpretato coerentemente con MOVE e nastro; se si desidera consumare la carta e proseguire, serve una diversa regola esplicita. Durante JUMP l’urto interrompe invece il salto.')
p('Supporto delle casse: gravità discreta senza cicli, arresto sul primo supporto. PROPOSTA: nastro e robot possono spingere una singola cassa usando la stessa validazione. Una cassa può occupare una piastra. Portali non trasferiscono casse. Caduta di una cassa su un robot causa fallimento per schiacciamento; nessuna fisica di rimbalzo.')
h('Oggetto temporizzato')
p('Stati proposti: inattivo → innescato con residuo N → trasformato oppure distrutto. Innesco alla prima spinta riuscita del robot; un tentativo fallito non innesca. Il passo di innesco non consuma il nuovo timer. Successive spinte non lo ricaricano. Il timer segue i cicli del personaggio anche se la cassa rimane ferma. L’attivazione da segnali è una possibile estensione, non inclusa nella base.')
table(['Variante proposta','Scadenza','Conseguenza'],[
('Cassa TNT','Esplode a residuo 0.','Rimuove la TNT; uccide robot e distrugge casse nelle celle bersaglio.'),
('Cassa indurente','Diventa immobile a residuo 0.','Mantiene posizione e supporto; le spinte successive falliscono.')])
h('Profilo TNT proposto')
p('Area iniziale: cella centrale e quattro celle ortogonali adiacenti, senza diagonali. Terreno, uscita e accessori restano intatti; casse ordinarie e indurite sono distruttibili. TNT colpite esplodono in catena nello stesso evento senza attendere il timer. Elaborare ogni TNT una sola volta in ordine di ID. L’esplosione non genera cicli; rimozioni di supporto possono causare cadute. Raggio, danni e catene richiedono validazione di design.')

page(9,'Interazioni di programmazione e ispezione')
p('CONFERMATO salvo dove indicato. Il giocatore modifica il programma soltanto nella fase di programmazione. L’inventario è un pannello inferiore richiamabile; contiene le carte disponibili nella stanza. Gli slot fissano il numero massimo di istruzioni, indipendentemente dal numero di carte disponibili.')
table(['Input o situazione','Risultato','Feedback proposto'],[
('Aprire il pannello inferiore','Mostra inventario e disponibilità.','Transizione breve; etichetta e conteggio.'),
('Trascinare inventario → slot libero','Colloca la carta.','Anteprima dello slot e conferma di aggancio.'),
('Trascinare slot → slot occupato','Scambia le due carte.','Anteprima di entrambe le posizioni.'),
('Trascinare carta fuori dalla zona','Restituisce la carta all’inventario.','Evidenza della restituzione e conteggio aggiornato.'),
('Inventario → sequenza completamente piena','Sostituisce la carta nell’ultimo slot.','Ultimo slot e carta restituita evidenziati prima del rilascio.'),
('Clic destro sulla carta nello slot','Alterna destra e sinistra; default destra.','Freccia e descrizione aggiornate.'),
('Play','Blocca programma e nasconde inventario.','Carta attiva e stato esecuzione visibili.')])
h('Completamento dei casi di trascinamento')
p('PROPOSTA. Gli slot hanno indice fisso: uno slot libero riceve la carta senza spostare gli altri; gli slot vuoti vengono saltati in esecuzione. Inventario su slot occupato con altri slot liberi sostituisce solo la carta bersaglio, restituendola all’inventario. Nessun inserimento con scorrimento. Escape durante drag annulla e ripristina la posizione iniziale. Queste scelte chiudono i casi lasciati aperti nella v0.3.')
h('Ispezione della stanza')
p('PROPOSTA. Puntatore o selezione mostra scheda con nome, stato, spostabilità, durata/residuo, conseguenza della scadenza e collegamenti. Selezionare un dispositivo evidenzia sorgenti e ricevitori con simboli condivisi. Le informazioni non richiedono l’avvio della simulazione. Le casse temporizzate mostrano separatamente durata iniziale e stato non ancora innescato.')
h('Input non validi')
p('PROPOSTA. Play senza carte resta disabilitato con motivo testuale. Input di modifica durante esecuzione non cambia lo stato; icona di blocco e messaggio breve «Torna alla programmazione per modificare». Evitare messaggi ripetuti per ogni frame o trascinamento. Non introdurre scorciatoie da tastiera non documentate o clic sugli oggetti che alterino direttamente la simulazione.')

page(10,'Esecuzione esiti e feedback')
h('Controlli disponibili')
p('CONFERMATO. Play, accelerazione per esempio x3, Skip e ritorno alla programmazione dopo un esito. PROPOSTA. Pulsante «Interrompi e modifica» durante esecuzione: annulla il tentativo, ripristina la stanza e conserva le carte. Accelerazione modifica soltanto la presentazione; Skip risolve gli stessi eventi logici fino al primo esito terminale, senza saltare controlli, timer o collisioni.')
h('Informazioni durante Play')
p('PROPOSTA. Evidenziare carta corrente, direzione e passi residui. Durante il nastro mantenere la carta evidenziata con etichetta «MOVE sospeso» e residuo visibile; durante caduta indicare «Caduta», senza incremento del ciclo. Mostrare contatore dei cicli e timer rilevanti. I passi consumati dalle carte e quelli prodotti dal nastro devono essere distinguibili nell’ispezione del tentativo.')
table(['Evento','Feedback visivo e sonoro proposto'],[
('Passo o spinta riuscita','Spostamento di una cella; cassa e robot animati insieme; breve suono meccanico.'),
('Presa in carico del nastro','Indicatore trasporto e MOVE sospeso; frecce del nastro attive.'),
('Urto durante salto','Arco interrotto, piccolo rimbalzo e ricaduta; nessun messaggio di fallimento.'),
('Movimento obbligatorio bloccato','Evidenza della cella ostacolo, cortocircuito/esplosione del robot e causa testuale.'),
('Innesco TNT o indurente','Contatore compare, simbolo specifico del futuro effetto e suono di armamento.'),
('Indurimento','Cambio di forma/materiale e icona immobile; nessun cambio di posizione.'),
('Morte o esplosione TNT','Effetto localizzato, sorgente evidenziata e spiegazione della catena causale.'),
('Successo','Evidenza dell’uscita, robot uscito e programma residuo non eseguito.')])
h('Esito e nuovo tentativo')
p('PROPOSTA. Il riepilogo mostra esito, carta e passo coinvolti, ciclo raggiunto e causa concreta: «MOVE 3 bloccato dalla cassa indurita» oppure «Timer esaurito: trappola attivata». Con Skip mantenere visibile il fotogramma logico dell’esito e i contatori finali. Tornare alla programmazione ricostruisce oggetti, segnali e timer iniziali, conservando ordine e direzioni delle carte.')
h('Accessibilità e chiarezza')
p('PROPOSTA. Stato comunicato da forma, icona e testo oltre al colore. Effetti rapidi non devono coprire celle o contatori. Audio disattivabile senza perdita di informazione. Riduzione del movimento per pannelli e impulsi; l’animazione semplificata deve comunque mostrare il percorso e il punto di errore. Nessuna vibrazione o scuotimento obbligatorio per capire il risultato.')

page(11,'Dati di livello e robot multipli')
h('Contratto dati proposto')
table(['Record','Campi essenziali'],[
('LevelDefinition','ID, dimensioni, terreno, limiti, robot iniziali, uscita, carte con quantità, numero slot, oggetti, collegamenti.'),
('ObjectDefinition','ID stabile, tipo, cella, ingombro, supporto, stato iniziale, spostabile, distruttibile, profilo interazioni.'),
('ConveyorDefinition','Celle, direzione orizzontale, enabled iniziale, ricevitore segnale.'),
('PortalPair','Due ID/celle, enabled iniziale, politica di arrivo occupato.'),
('SwitchDefinition','Tipo, cella di pressione, stato iniziale, durata cicli se temporizzato.'),
('TimedObjectDefinition','Durata, modalità di innesco, effetto scadenza, distruttibilità, eventuale area esplosione.'),
('SignalLink','ID sorgente, ID ricevitore e polarità.'),
('RuntimeState','Snapshot separato dalle definizioni: celle correnti, residui, segnali, carte e stati terminali.')])
h('Validazione prima di Play')
p('PROPOSTA. Rifiutare ID duplicati, riferimenti mancanti, portali senza partner, quantità negative, durate non positive, sovrapposizioni solide e partenze non valide. Segnalare circuiti di nastri senza uscita nel controllo di design; la raggiungibilità dipendente da timer richiede anche prove di simulazione. Il giocatore non deve incontrare livelli intenzionalmente bloccati da questi errori.')
h('Determinismo e diagnostica')
p('PROPOSTA. Motore logico indipendente da MonoBehaviour, rigidbody e animazioni. Ogni evento registra indice, robot, causa, celle origine/destinazione, variazioni dei residui e dei segnali. Confrontare snapshot a parità di programma nelle modalità normale, x3 e Skip. Un rilevamento di stato identico senza progresso è una protezione tecnica per gli autori, non una meccanica di puzzle.')
h('Robot multipli')
p('CONFERMATO. Il programma è condiviso. APERTO. Sincronizzazione delle carte, collisioni reciproche, proprietà dei timer condivisi e condizione di vittoria definitiva. La v0.3 propone che tutti raggiungano l’uscita, ma non lo stabilisce definitivamente. Le regole operative delle pagine precedenti descrivono il caso con un robot.')
p('PROPOSTA da valutare per estensione: avanzamento a barriere tra carte, con ogni robot che completa carta e conseguenze prima dell’avvio comune della successiva; robot usciti rimossi dall’occupazione. Rimane necessaria una scelta su cicli globali o per robot: due spostamenti simultanei non devono decrementare accidentalmente un timer due volte. Non implementare questa estensione assumendo un ordine arbitrario degli ID.')

page(12,'Accettazione e decisioni da validare')
h('Casi di accettazione del comportamento concordato')
table(['Scenario','Risultato atteso'],[
('MOVE 4 senza ostacoli','Quattro cicli, residuo zero, poi carta successiva.'),
('Passo oltre bordo e caduta di 5 celle','Un ciclo per il passo; zero per la caduta.'),
('JUMP 3 completato','Tre celle verticali e una orizzontale; un ciclo.'),
('Ingresso nel portale da carta o nastro','Conta il passo di ingresso; teletrasporto senza ciclo extra.'),
('MOVE sospeso con residuo 3; nastro trasporta 2 celle','Due cicli timer; residuo MOVE ancora 3.'),
('Il timer spegne il nastro sotto il robot','Fine trasporto e ripresa del MOVE residuo.'),
('Ostacolo impedisce il passo del nastro','Nessun passo riuscito; crash e fine tentativo.'),
('Robot raggiunge botola aperta','Caduta immediata senza cicli verticali.'),
('Stesso programma a normale, x3 e Skip','Stesso stato finale, stessi timer e stessa causa di esito.')])
h('Casi aggiuntivi per le proposte tecniche')
p('Verificare: passo che innesca un timer non lo decrementa; cassa spinta non raddoppia il ciclo; indurimento dopo una spinta non la annulla; TNT in catena esplodono una volta ciascuna; piastra liberata e occupata nella stessa spinta ha segnali coerenti; carta a residuo zero attende il trasporto; reset ripristina botole, casse distrutte, timer e collegamenti.')
h('Decisioni ancora da approvare')
p('Priorità alta: fine carta o uscita dalla cella per il cedimento; caduta durante MOVE e ripresa del residuo; chiusura di botola con vano occupato; innesco e area TNT; risposta al contatto con oggetti immobili; timer degli oggetti che seguono i cicli del robot. Queste scelte sono specificate come proposte, non come accordi già raggiunti.')
p('Priorità successiva: casse sui nastri e sulle botole, portali occupati, tipi di pulsanti oltre al temporizzato, aggregazione OR e polarità, gestione degli slot vuoti, conflitto morte/uscita nello stesso evento. Robot multipli richiedono una specifica dedicata prima della produzione di livelli che ne dipendano.')
h('Modifiche rispetto alla versione 0.3')
p('Aggiunti ciclo orizzontale, sospensione e ripresa di MOVE sui nastri, timer a cicli, due famiglie di botole, rete di interruttori, categorie di oggetti e varianti temporizzate. Estese programmazione, ispezione, feedback e diagnostica. Formalizzati ordine degli eventi, dati e casi di accettazione, mantenendo esplicite le decisioni non ancora approvate.')

footer=s.footer.paragraphs[0]
footer.alignment=2
r=footer.add_run('Robot Run · GDD 0.4 · ');r.font.size=Pt(8)
fld=OxmlElement('w:fldSimple');fld.set(qn('w:instr'),'PAGE');footer._p.append(fld)
d.core_properties.title='GDD tecnico delle meccaniche Robot Run'
d.core_properties.subject='Movimento, ambiente, interazioni e feedback'
d.save(OUT)
print(OUT)
