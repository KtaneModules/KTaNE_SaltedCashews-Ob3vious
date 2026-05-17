using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class SaltedCashewsScript : MonoBehaviour
{
    public const int PROGRESS_GOAL = 5;

    public Transform NutMain;
    public List<MeshRenderer> Salt;
    public List<MeshRenderer> Nut;
    public List<KMSelectable> Buttons;

    private int _minSalt;
    private int _maxSalt;
    private float _minRoast;
    private float _maxRoast;
    private int _maxImbalance;

    private int[] _currentSalt;
    private float _currentRoast;
    private bool _currentValidity;

    private int _progress;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        GenerateRuleBounds();
        GenerateNut();

        for (int i = 0; i < 3; i++)
        {
            int i2 = i;
            Buttons[i].OnInteract += () =>
            {
                Buttons[i2].AddInteractionPunch();
                GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[i2].transform);
                Buttons[i2].transform.localPosition = new Vector3(Buttons[i2].transform.localPosition.x, 0.01f, Buttons[i2].transform.localPosition.z);

                if (_progress >= PROGRESS_GOAL)
                    return false;

                if (i2 == 1)
                {
                    NutMain.transform.localEulerAngles += new Vector3(0, 0, 180);
                    return false;
                }

                Log("The cashew has been sent to {0} Eltrick.", i2 == 0 ? "real" : "fake");

                if (i2 == 0)
                    _progress += _currentValidity ? 1 : 2;

                if (!_currentValidity)
                    StartCoroutine(Fuck(i2 == 2));
                else if (i2 == 0)
                    StartCoroutine(Approve());

                if (_progress >= PROGRESS_GOAL)
                {
                    Salt.ForEach(x => x.enabled = false);
                    Nut.ForEach(x => x.enabled = false);
                }
                else
                    GenerateNut();

                return false;
            };

            Buttons[i].OnInteractEnded += () =>
            {
                Buttons[i2].transform.localPosition = new Vector3(Buttons[i2].transform.localPosition.x, 0.015f, Buttons[i2].transform.localPosition.z);
                GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, Buttons[i2].transform);
            };
        }
    }

    private IEnumerator Fuck(bool fake)
    {
        yield return new WaitForSeconds(0.5f);
        GetComponent<KMAudio>().PlaySoundAtTransform(fake ? "FAKE" : "FUCK", transform);
        if (fake)
            yield break;
        Log("Eltrick seems not too fond of this nut. Strike!");
        yield return new WaitForSeconds(0.5f);
        GetComponent<KMBombModule>().HandleStrike();
        yield return Approve();
    }

    private IEnumerator Approve()
    {
        if (_progress >= PROGRESS_GOAL)
        {
            yield return new WaitForSeconds(0.5f);
            Log("Eltrick has finished snacking. Module solved!");
            GetComponent<KMBombModule>().HandlePass();
        }
    }

    private bool ValidateNut()
    {
        bool saltAmount = _minSalt <= _currentSalt.Sum() && _maxSalt >= _currentSalt.Sum();
        bool roastLevel = _minRoast <= _currentRoast && _maxRoast >= _currentRoast;
        bool distribution = Mathf.Abs(_currentSalt[0] - _currentSalt[1]) <= _maxImbalance;

        if (!saltAmount)
            Log("The amount of salt on this cashew is too {0}.", _currentSalt.Sum() < _minSalt ? "little" : "much");
        if (!roastLevel)
            Log("This cashew is roasted too {0}.", _currentRoast < _minRoast ? "little" : "much");
        if (!distribution)
            Log("The salt distribution in this cashew is awful.");

        Log("The cashew {0} quality testing.", saltAmount && roastLevel && distribution ? "passes" : "does not pass");

        return saltAmount && roastLevel && distribution;
    }

    private void GenerateNut()
    {
        int totalSalt = UnityEngine.Random.Range(Mathf.Max(0, _minSalt - (int)Mathf.Ceil((_maxSalt - _minSalt) / 6f)), Mathf.Min(21, 1 + _maxSalt + (int)Mathf.Ceil((_maxSalt - _minSalt) / 6f)));
        _currentRoast = UnityEngine.Random.Range(Mathf.Max(0, _minRoast - (_maxRoast - _minRoast) / 6f), Mathf.Min(1, _maxRoast + (_maxRoast - _minRoast) / 6f));
        int imbalance = UnityEngine.Random.Range(0, Mathf.Min(totalSalt, _maxImbalance + (int)Mathf.Ceil(_maxImbalance / 6f)) + 1);
        if (imbalance % 2 != totalSalt % 2)
            imbalance--;
        _currentSalt = new int[2];
        _currentSalt[UnityEngine.Random.Range(0, 2)] = imbalance;
        totalSalt -= imbalance;
        for (int i = 0; i < _currentSalt.Length; i++)
            _currentSalt[i] += totalSalt / 2;

        SaltNut(_currentSalt[0], _currentSalt[1]);
        RoastNut(_currentRoast);

        NutMain.transform.localEulerAngles = new Vector3(0, UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0, 2) * 180);
        NutMain.transform.localPosition = new Vector3(UnityEngine.Random.Range(-0.06f, 0.02f), NutMain.transform.localPosition.y, UnityEngine.Random.Range(-0.06f, 0.06f));

        Log("A new cashew appears! One side has {0} grains of salt and the other side has {1}. It's roasted approximately {2}%.", _currentSalt.Min(), _currentSalt.Max(), Mathf.Round(_currentRoast * 100));

        _currentValidity = ValidateNut();
    }

    private void GenerateRuleBounds()
    {
        //salt range: 0-20, at least 10 acceptable
        _minSalt = UnityEngine.Random.Range(0, 15);
        _maxSalt = UnityEngine.Random.Range(0, 15);
        if (_maxSalt < _minSalt)
        {
            _minSalt = _minSalt - _maxSalt;
            _maxSalt = _minSalt + _maxSalt;
            _minSalt = _maxSalt - _minSalt;
        }
        _maxSalt += 5;
        Log("Rule: A cashew of quality should have between {0} and {1} grains of salt (both bounds inclusive).", _minSalt, _maxSalt);

        //roast range: 0-1, at least 0.25 acceptable, at least 0.25 unacceptable
        _minRoast = UnityEngine.Random.Range(0, 0.5f);
        _maxRoast = UnityEngine.Random.Range(0, 0.5f);
        if (_maxRoast < _minRoast)
        {
            _minRoast = _minRoast - _maxRoast;
            _maxRoast = _minRoast + _maxRoast;
            _minRoast = _maxRoast - _minRoast;
        }
        _maxRoast += 0.25f;
        float offset = UnityEngine.Random.Range(0, 0.25f);
        _minRoast += offset;
        _maxRoast += offset;
        Log("Rule: A cashew of quality should be roasted between approximately {0}% and {1}%.", Mathf.Round(_minRoast * 100), Mathf.Round(_maxRoast * 100));

        //imbalance range: 1-5
        _maxImbalance = UnityEngine.Random.Range(1, 6);
        Log("Rule: A cashew of quality should have at most a difference of {0} grains of salt between its sides.", _maxImbalance);
    }

    private void RoastNut(float intensity)
    {
        Color color = new Color(0.625f - intensity * 0.325f, 0.5f - intensity * 0.325f, 0.325f - intensity * 0.325f);
        foreach (MeshRenderer halfNut in Nut)
            halfNut.material.color = color;
    }

    private void SaltNut(int top, int bottom)
    {
        List<MeshRenderer> usedGrains = new List<MeshRenderer>();
        while (Salt.Count < top + bottom)
            Salt.Add(Instantiate(Salt[0], Salt[0].transform.parent));
        Queue<MeshRenderer> usableGrains = new Queue<MeshRenderer>(Salt);

        for (int i = 0; i < top; i++)
        {
            MeshRenderer grain = usableGrains.Dequeue();
            grain.enabled = true;
            while (!TryRandomiseSaltPosition(true, grain, usedGrains)) ;
            usedGrains.Add(grain);
        }
        for (int i = 0; i < bottom; i++)
        {
            MeshRenderer grain = usableGrains.Dequeue();
            grain.enabled = true;
            while (!TryRandomiseSaltPosition(false, grain, usedGrains)) ;
            usedGrains.Add(grain);
        }

        foreach (MeshRenderer grain in usableGrains)
        {
            grain.enabled = false;
        }
    }

    private bool TryRandomiseSaltPosition(bool top, MeshRenderer salt, IEnumerable<MeshRenderer> avoid)
    {
        float majorRadius = 0.00625f;
        float minorRadius = 0.01f - majorRadius;

        float saltLeniency = 0.0003125f;
        float saltRepulsion = 0.00125f;

        float xbound = 0.006f;
        float zbound = 0.01f;
        float ybound = 0.005f;

        float x = UnityEngine.Random.Range(-xbound, xbound);
        float z = UnityEngine.Random.Range(-zbound, zbound);
        float y = UnityEngine.Random.Range(0.000625f, ybound);

        float xFromCentre = x + 0.005f;
        if (Mathf.Abs(Mathf.Atan2(z, xFromCentre)) > Mathf.PI / 4f)
        {
            Vector3 endSphereCentre = new Vector3(Mathf.Sin(Mathf.PI / 4f), 0, Mathf.Cos(Mathf.PI / 4f)) * majorRadius;
            Vector3 sphereDistanceVector = new Vector3(xFromCentre, y - 0.000625f, Mathf.Abs(z)) - endSphereCentre;

            if (Mathf.Abs(sphereDistanceVector.magnitude - minorRadius) > saltLeniency)
                return false;
        }
        else
        {
            Vector3 majorRingPoint = new Vector3(xFromCentre, 0, z).normalized * majorRadius;
            Vector3 minorRingVector = new Vector3(xFromCentre, y - 0.000625f, z) - majorRingPoint;

            if (Mathf.Abs(minorRingVector.magnitude - minorRadius) > saltLeniency)
                return false;
        }
        Vector3 position = new Vector3(x, top ? y : -y, z);

        if (avoid.Any(w => (w.transform.localPosition - position).magnitude < saltRepulsion))
            return false;

        salt.transform.localPosition = position;

        Vector3 angleVector = new Vector3(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
        float yAngle = Mathf.Atan2(angleVector.z, angleVector.x);
        float xAngle = Mathf.Atan2(angleVector.y, Mathf.Sqrt(angleVector.x * angleVector.x + angleVector.z * angleVector.z));

        salt.transform.localEulerAngles = new Vector3(xAngle * Mathf.Rad2Deg, yAngle * Mathf.Rad2Deg, 0);

        return true;
    }

    private void Log(string text, params object[] args)
    {
        Debug.LogFormat("[Salted Cashews #{0}] {1}", _moduleId, string.Format(text, args));
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "'!{0} press send' to send the cashew to Eltrick. '!{0} press flip' to turn over the cashew. '!{0} press test' to send the cashew to testing.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        command = command.ToLowerInvariant();
        switch (command)
        {
            case "press send":
                Buttons[0].OnInteract();
                yield return new WaitForSeconds(0.1f);
                Buttons[0].OnInteractEnded();
                break;
            case "press flip":
                Buttons[1].OnInteract();
                yield return new WaitForSeconds(0.1f);
                Buttons[1].OnInteractEnded();
                break;
            case "press test":
                Buttons[2].OnInteract();
                yield return new WaitForSeconds(0.1f);
                Buttons[2].OnInteractEnded();
                break;
            default:
                yield return "sendtochaterror Invalid command.";
                yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (_progress < PROGRESS_GOAL)
        {
            int press = _currentValidity ? 0 : 2;
            Buttons[press].OnInteract();
            yield return new WaitForSeconds(0.1f);
            Buttons[press].OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(1f);
    }
}
