using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NullGameEntity : BaseGameEntity
{
    public Slider healthSlider;

    public NullGameEntity(int id) : base(id)
    {
    }

    // Start is called before the first frame update
    public override void Start()
    {
        MaxHealth = 100;
        healthSlider.value = maxHealth;
    }

    // Update is called once per frame
    public override void Update()
    {
        healthSlider.value = health;
    }

    public override bool HandleMessage(Telegram telegram)
    {
        return false;
    }

}
