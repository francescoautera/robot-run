using UnityEngine;

namespace RobotRun
{
    [CreateAssetMenu(fileName = "DeviceTooltipText", menuName = "Robot Run/Testi tooltip dei dispositivi")]
    public sealed class DeviceTooltipText : ScriptableObject
    {
        internal const string ResourceName = "DeviceTooltipText";

        [Header("Portali e pedana monouso")]
        [SerializeField] private string portaleAttivo = "Portale attivo";
        [SerializeField] private string portaleSpento = "Portale spento";
        [SerializeField] private string pedanaPortalePronta = "Pedana monouso · attiva il portale";
        [SerializeField] private string pedanaPortaleAttiva = "Pedana monouso attivata";

        [Header("Nastri e pulsanti")]
        [Tooltip("Usa {direzione} e {stato} per inserire i valori correnti del nastro.")]
        [SerializeField] private string nastro = "Nastro {direzione} · {stato}";
        [SerializeField] private string versoDestra = "a destra";
        [SerializeField] private string versoSinistra = "a sinistra";
        [SerializeField] private string statoAttivo = "attivo";
        [SerializeField] private string statoSpento = "spento";
        [SerializeField] private string piastra = "Piastra a pressione · {stato}";
        [SerializeField] private string piastraPremuta = "premuta";
        [SerializeField] private string piastraLibera = "libera";
        [SerializeField] private string pulsanteAScatto = "Pulsante a scatto · {stato}";
        [SerializeField] private string pulsanteMonouso = "Pulsante monouso · {stato}";
        [SerializeField] private string monousoPronto = "pronto";
        [SerializeField] private string monousoAttivato = "attivato";
        [Tooltip("Usa {durata} per i cicli totali e {residuo} per i cicli rimanenti.")]
        [SerializeField] private string pulsanteATimer = "Pulsante a timer · {durata} cicli";

        [Header("Botole e trappole")]
        [SerializeField] private string botolaFragile = "Botola a cedimento · {stato}";
        [SerializeField] private string botolaComandata = "Botola comandata · {stato}";
        [SerializeField] private string botolaAperta = "aperta";
        [SerializeField] private string botolaChiusa = "chiusa";
        [SerializeField] private string botolaArmata = "armata";
        [SerializeField] private string botolaIntegra = "integra";
        [SerializeField] private string botolaInAttesa = "chiusura in attesa";
        [SerializeField] private string trappola = "Trappola · {stato}";
        [SerializeField] private string trappolaLetale = "letale";
        [SerializeField] private string trappolaSpenta = "spenta";

        [Header("Casse e ostacoli")]
        [SerializeField] private string cassaMobile = "Cassa spostabile";
        [SerializeField] private string bloccoFisso = "Blocco immovibile";
        [SerializeField, TextArea(2, 4)] private string cassaTnt = "Cassa TNT · esplode dopo {durata} cicli dalla prima spinta";
        [SerializeField, TextArea(2, 4)] private string cassaIndurente = "Cassa indurente · si blocca dopo {durata} cicli dalla prima spinta";
        [SerializeField] private string cassaIndurita = "Cassa indurita · immovibile";

        [Header("Contatori e collegamenti")]
        [Tooltip("Aggiunto quando restano cicli, se il testo del dispositivo non contiene già {residuo}.")]
        [SerializeField] private string contatoreResiduo = "residuo {residuo}";
        [SerializeField] private string collegamenti = "Collegato a {collegamenti}";
        [SerializeField] private string collegamentoInvertito = "{oggetto} (invertito)";
        [SerializeField] private string nomePortale = "portale";
        [SerializeField] private string nomePedanaPortale = "pedana monouso";

        internal string Portal(bool active) => active ? portaleAttivo : portaleSpento;
        internal string PortalPlate(bool active) => active ? pedanaPortaleAttiva : pedanaPortalePronta;

        internal string Device(DeviceState device)
        {
            string state = "";
            string template;
            switch (device.Definition.Kind)
            {
                case DeviceKind.Conveyor:
                    template = nastro;
                    state = device.Active ? statoAttivo : statoSpento;
                    break;
                case DeviceKind.PressurePlate:
                    template = piastra;
                    state = device.Active ? piastraPremuta : piastraLibera;
                    break;
                case DeviceKind.ToggleSwitch:
                    template = pulsanteAScatto;
                    state = device.Active ? statoAttivo : statoSpento;
                    break;
                case DeviceKind.OneShotSwitch:
                    template = pulsanteMonouso;
                    state = device.Active ? monousoAttivato : monousoPronto;
                    break;
                case DeviceKind.TimedSwitch:
                    template = pulsanteATimer;
                    break;
                case DeviceKind.FragileHatch:
                    template = botolaFragile;
                    state = device.Active ? botolaAperta : device.Armed ? botolaArmata : botolaIntegra;
                    break;
                case DeviceKind.SignalHatch:
                    template = botolaComandata;
                    state = device.PendingClose ? botolaInAttesa : device.Active ? botolaAperta : botolaChiusa;
                    break;
                case DeviceKind.Trap:
                    template = trappola;
                    state = device.Active ? trappolaLetale : trappolaSpenta;
                    break;
                case DeviceKind.Tnt:
                    template = cassaTnt;
                    break;
                case DeviceKind.HardeningCrate:
                    template = device.Hardened ? cassaIndurita : cassaIndurente;
                    break;
                case DeviceKind.FixedBlock:
                    template = bloccoFisso;
                    break;
                default:
                    template = cassaMobile;
                    break;
            }

            template ??= "";
            string description = template.Replace("{stato}", state ?? "")
                .Replace("{direzione}", (device.Definition.Direction > 0 ? versoDestra : versoSinistra) ?? "")
                .Replace("{durata}", device.Definition.Duration.ToString())
                .Replace("{residuo}", device.Remaining.ToString());
            if (device.Remaining <= 0 || template.Contains("{residuo}") || string.IsNullOrWhiteSpace(contatoreResiduo))
                return description;

            string counter = contatoreResiduo.Replace("{residuo}", device.Remaining.ToString());
            return string.IsNullOrEmpty(description) ? counter : $"{description} · {counter}";
        }

        internal string LinkEndpoint(string id, bool inverted)
        {
            var name = id switch
            {
                "portal" => nomePortale,
                "legacyPlate" => nomePedanaPortale,
                _ => id
            };
            return inverted ? (collegamentoInvertito ?? "").Replace("{oggetto}", name ?? "") : name ?? "";
        }

        internal string Links(string names) => (collegamenti ?? "").Replace("{collegamenti}", names);
    }
}
