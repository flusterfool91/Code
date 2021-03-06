﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Schema;
using System.Timers;

namespace Spot
{
    class Player : MoveableObject
    {
        ContentManager myContent;
        SpriteBatch mySpriteBatch;

        //variables and properties
        public enum PlayerState
        {
            Jumping,
            Boosting,
            Running,
            Idle,
            Attacking,
            Dead,
            Hitstun
        }
        public PlayerState myState, previousState;
        KeyboardState myKeyState, previousKeyState;
        GamePadState previousButtonState;
        XmlDocument myStats;

        //////////
        //X Move variables
        //////////
        bool leftMove = false;
        bool rightMove = false;
        bool upMove = false;
        float runningDecelRate;//5f
        //////////
        //Jump variables
        //////////
        bool canUpPress = true;
        int jumpSpeedLimit;//10
        int jumpSpeed;//-13
        int fallSpeed;//10
        int wallJumpSpeed;//-8
        int doubleJumpSpeed;//-9
        int jumpCount;//0
        //////////
        //Attack variables
        //////////
        public bool canAttack = true;
        bool canJpress = true;
        bool canKpress = true;
        //////////
        //Puzzle Check variables
        //////////
        int totalPieces;
        int totalCircles;
        int totalTriangle;
        int totalSquare;

        String lightRight;
        String lightLeft;
        LightAttack lAttack = null;

        GamePadState padState;

        public Player(Vector2 newPos)
        {
            myStats = new XmlDocument();
            myStats.Load("Content/XML/PlayerSample.xml");
            XmlNode playerNode = myStats.FirstChild;

            health = int.Parse(playerNode.Attributes.GetNamedItem("health").Value);
            height = int.Parse(playerNode.Attributes.GetNamedItem("height").Value);
            width = int.Parse(playerNode.Attributes.GetNamedItem("width").Value);
            maxSpeed = int.Parse(playerNode.Attributes.GetNamedItem("maxSpeed").Value);
            accelRate = float.Parse(playerNode.Attributes.GetNamedItem("accelRate").Value);
            decelRate = float.Parse(playerNode.Attributes.GetNamedItem("decelRate").Value);
            jumpSpeedLimit = int.Parse(playerNode.Attributes.GetNamedItem("jumpSpeedLimit").Value);
            jumpSpeed = int.Parse(playerNode.Attributes.GetNamedItem("jumpSpeed").Value);
            fallSpeed = int.Parse(playerNode.Attributes.GetNamedItem("fallSpeed").Value);
            gravity = float.Parse(playerNode.Attributes.GetNamedItem("gravity").Value);

            position = newPos;
            speed = new Vector2(0, 0);
            currentAccel = 0;

            idleAnim = "Player/HeroIdleRight";
            idleLeftAnim = "Player/HeroIdleLeft";
            runAnim = "Player/HeroWalkRight";
            runLeftAnim = "Player/HeroWalkLeft";
            lightRight = "Player/HeroAttackRight";
            lightLeft = "Player/HeroAttackLeft";
            //midRight = "robo_running";
            //midLeft = "robo_running_left";
            //heavyRight = "Player/HeroHeavyRight";
            //heavyLeft = "Player/HeroHeavyLeft";
            hurtLeft = "Player/HeroLeftHurt";
            hurtRight = "Player/HeroRightHurt";
            jumpLeft = "Player/HeroJumpLeft";
            jumpRight = "Player/HeroJumpRight";
        }

        public void LoadContent(ContentManager content)
        {
            currentAnimation = idleAnim;
            texture = content.Load<Texture2D>(currentAnimation);
            animationRect = new Rectangle(0, 0, width, height);
            animTimer.Elapsed += new ElapsedEventHandler(UpdateAnimation);
            animTimer.Enabled = true;

            jumpCount = 0;
            myState = PlayerState.Idle;
            myContent = content;
        }

        public override void Update()
        {
            myKeyState = Keyboard.GetState();
            padState = GamePad.GetState(PlayerIndex.One);
            if (myState != PlayerState.Dead)
            {
                if (Game1.Instance().usingController)
                {
                    checkKeysDown(padState);
                    checkKeysUp(padState);
                }
                else
                {
                    checkKeysDown(myKeyState);
                    checkKeysUp(myKeyState);
                }
                UpdateMovement(myKeyState);
                UpdateTexture();
            }

            if (health < 1)
            {
                myState = PlayerState.Dead;
                controlsLocked = true;
                death();
            }
        }

        public void death()
        {
            LevelManager.Instance().restartLevel();
        }

        public void UpdateTexture()
        {
            if (!hitstun)
            {
                if (myState == PlayerState.Running)
                {
                    if (facing == 0 && currentAnimation != runAnim)
                    {
                        animationRect = new Rectangle(0, 0, width, height);
                        texture = myContent.Load<Texture2D>(runAnim);
                        currentAnimation = runAnim;

                    }
                    else if (facing == 1 && currentAnimation != runLeftAnim)
                    {
                        animationRect = new Rectangle(0, 0, width, height);
                        texture = myContent.Load<Texture2D>(runLeftAnim);
                        currentAnimation = runLeftAnim;
                    }
                }
                else if (myState == PlayerState.Idle)
                {
                    if (facing == 0 && currentAnimation != idleAnim)
                    {
                        animationRect = new Rectangle(0, 0, width, height);
                        texture = myContent.Load<Texture2D>(idleAnim);
                        currentAnimation = idleAnim;
                    }
                    else if (facing == 1 && currentAnimation != idleLeftAnim)
                    {
                        animationRect = new Rectangle(0, 0, width, height);
                        texture = myContent.Load<Texture2D>(idleLeftAnim);
                        currentAnimation = idleLeftAnim;
                    }
                }
                else if (myState == PlayerState.Jumping)
                {
                    if (facing == 0 && currentAnimation != jumpRight)
                    {
                        animationRect = new Rectangle(0, 0, width, height);
                        texture = myContent.Load<Texture2D>(jumpRight);
                        currentAnimation = jumpRight;
                    }
                    else if (facing == 1 && currentAnimation != jumpLeft)
                    {
                        animationRect = new Rectangle(0, 0, width, height);
                        texture = myContent.Load<Texture2D>(jumpLeft);
                        currentAnimation = jumpLeft;
                    }
                }
                else if (myState == PlayerState.Attacking)//light attack
                {
                    //attack animations here
                    if (facing == 0 && currentAnimation != lightRight)
                    {
                        animationRect = new Rectangle(0, 0, width, height);
                        texture = myContent.Load<Texture2D>(lightRight);
                        currentAnimation = lightRight;
                    }
                    else if (facing == 1 && currentAnimation != lightLeft)
                    {
                        animationRect = new Rectangle(0, 0, width, height);
                        texture = myContent.Load<Texture2D>(lightLeft);
                        currentAnimation = lightLeft;
                    }
                }
            }
            else if (hitstun)
            {
                if (facing == 1 && currentAnimation != hurtLeft)
                {
                    animationRect = new Rectangle(0, 0, width, height);
                    texture = myContent.Load<Texture2D>(hurtLeft);
                    currentAnimation = hurtLeft;
                }
                else if (facing == 0 && currentAnimation != hurtRight)
                {
                    animationRect = new Rectangle(0, 0, width, height);
                    texture = myContent.Load<Texture2D>(hurtRight);
                    currentAnimation = hurtRight;
                }
            }
        }

        public void lockPlayerControls()
        {
            if (!controlsLocked)
            {
                currentAccel = 0;
                controlsLocked = true;
            }
        }

        public void unlockPlayerControls()
        {
            if (controlsLocked)
                controlsLocked = false;
        }

        public void checkKeysDown(KeyboardState keyState)
        {
            if (!controlsLocked && myState != PlayerState.Hitstun)
            {
                if (keyState.IsKeyDown(Keys.D) == true && previousKeyState.IsKeyDown(Keys.D) == true && myState != PlayerState.Attacking)
                {
                    rightMove = true;
                    facing = 0;
                }
                if (keyState.IsKeyDown(Keys.A) == true && previousKeyState.IsKeyDown(Keys.A) == true && myState != PlayerState.Attacking)
                {
                    leftMove = true;
                    facing = 1;
                }
                if (keyState.IsKeyDown(Keys.W) == true && previousKeyState.IsKeyDown(Keys.W) == true && myState != PlayerState.Jumping)
                {
                    if (!controlsLocked && myState != PlayerState.Attacking && canUpPress)
                    {
                        myState = PlayerState.Jumping;
                        position.Y -= 2;
                        speed.Y = jumpSpeed;
                        canUpPress = false;
                    }
                }
                if (keyState.IsKeyDown(Keys.J) == true && previousKeyState.IsKeyDown(Keys.J) == true)
                {
                    if (canAttack && CheckCollision(BottomBox) && canJpress)
                    {
                        //attack code
                        currentAccel = 0;
                        speed.X = 0;
                        myState = PlayerState.Attacking;
                        //controlsLocked = true;
                        canAttack = false;
                        canJpress = false;
                        attack();
                    }
                }
                if (keyState.IsKeyDown(Keys.K) == true && previousKeyState.IsKeyDown(Keys.K) == true)
                {
                    if (CheckCollision(BottomBox) && canAttack && canKpress)
                    {
                        //k button code
                    }
                }

            }
            previousKeyState = keyState;
        }

        public void checkKeysDown(GamePadState keyState)
        {
            if (!controlsLocked && myState != PlayerState.Hitstun)
            {
                if (keyState.ThumbSticks.Left.X >= 0.3 /* && previousKeyState.IsKeyDown(Keys.D) == true */&& myState != PlayerState.Attacking)
                {
                    rightMove = true;
                    facing = 0;
                }
                if (keyState.ThumbSticks.Left.X <= -0.3 /* && previousKeyState.IsKeyDown(Keys.A) == true*/ && myState != PlayerState.Attacking)
                {
                    leftMove = true;
                    facing = 1;
                }
                if (keyState.IsButtonDown(Buttons.A) == true && previousButtonState.IsButtonDown(Buttons.A) == true && myState != PlayerState.Jumping)
                {
                    if (!controlsLocked && myState != PlayerState.Attacking && canUpPress)
                    {
                        myState = PlayerState.Jumping;
                        position.Y -= 2;
                        speed.Y = jumpSpeed;
                        canUpPress = false;
                    }
                }
                if (keyState.IsButtonDown(Buttons.X) == true && previousButtonState.IsButtonDown(Buttons.X) == true)
                {
                    if (canAttack && CheckCollision(BottomBox) && canJpress)
                    {
                        currentAccel = 0;
                        speed.X = 0;
                        myState = PlayerState.Attacking;
                        //controlsLocked = true;
                        canAttack = false;
                        canJpress = false;
                        attack();
                    }
                }
                if (keyState.IsButtonDown(Buttons.Y) == true && previousButtonState.IsButtonDown(Buttons.Y) == true)
                {
                    if (CheckCollision(BottomBox) && canAttack && canKpress)
                    {
                       
                    }
                }
            }
            previousButtonState = keyState;
        }

        public void checkKeysUp(KeyboardState keyState)
        {
            if (myState != PlayerState.Hitstun)
            {
                if (keyState.IsKeyUp(Keys.J) == true)
                {
                    if (canJpress == false)
                        canJpress = true;
                }
                if (keyState.IsKeyUp(Keys.K) == true)
                {
                    if (canKpress == false)
                        canKpress = true;
                }
            }

            if (!controlsLocked && myState != PlayerState.Hitstun && myState != PlayerState.Attacking)
            {
                if (keyState.IsKeyUp(Keys.D) == true)
                {
                    rightMove = false;
                    currentAccel = 0;
                }
                if (keyState.IsKeyUp(Keys.A) == true)
                {
                    leftMove = false;
                    currentAccel = 0;
                }
                if (keyState.IsKeyUp(Keys.W) == true)
                {
                    canUpPress = true;
                }
                //if (keyState.IsKeyUp(Keys.J) == true)
                //{
                //    if (canJpress == false)
                //        canJpress = true;
                //}

                if (!leftMove && !rightMove && CheckCollision(BottomBox))
                {
                    myState = PlayerState.Idle;
                }
            }
        }

        public void checkKeysUp(GamePadState keyState)
        {
            if (myState != PlayerState.Hitstun)
            {
                if (keyState.IsButtonUp(Buttons.X) == true)
                {
                    if (canJpress == false)
                        canJpress = true;
                }
                if (keyState.IsButtonUp(Buttons.Y) == true)
                {
                    if (canKpress == false)
                        canKpress = true;
                }
            }

            if (!controlsLocked && myState != PlayerState.Hitstun && myState != PlayerState.Attacking)
            {
                if (keyState.ThumbSticks.Left.X <= 0.3 && keyState.ThumbSticks.Left.X >= 0)
                {
                    rightMove = false;
                    currentAccel = 0;
                }
                if (keyState.ThumbSticks.Left.X >= -0.3 && keyState.ThumbSticks.Left.X <= 0)
                {
                    leftMove = false;
                    currentAccel = 0;
                }
                if (keyState.IsButtonUp(Buttons.A))
                {
                    canUpPress = true;
                }

                if (!leftMove && !rightMove && CheckCollision(BottomBox))
                {
                    myState = PlayerState.Idle;
                }
            }
        }

        public void UpdateMovement(KeyboardState keyState)
        {
            #region Xmovement
            //if (!hitstun)
            //{
            if (rightMove && !controlsLocked && myState != PlayerState.Attacking)
            {

                if (CheckCollision(BottomBox))
                {
                    myState = PlayerState.Running;
                }

                if (!CheckCollision(RightBox))
                {
                    currentAccel = accelRate;
                }
            }
            if (leftMove && !controlsLocked && myState != PlayerState.Attacking)
            {
                if (CheckCollision(BottomBox))
                {
                    myState = PlayerState.Running;
                }

                if (!CheckCollision(LeftBox))
                {
                    currentAccel = -accelRate;
                }
            }
            //}
            #endregion

            #region gravity
            //makes play fall off ledges when he walks off
            if (!CheckCollision(BottomBox))
            {
                myState = PlayerState.Jumping;
            }
            else
            {
                if (collidingWall.isEnemy)
                {
                    if (position.X < collidingWall.position.X && speed.Y > 0)
                    {
                        //collidingWall.position.X = RightBox.X + RightBox.Width;
                        collidingWall.position.X += 15;
                    }
                    else if (position.X > collidingWall.position.X && speed.Y > 0)
                    {
                        //collidingWall.position.X = position.X - 60;
                        collidingWall.position.X -= 15;
                    }
                }
                else
                {
                    position.Y = collidingWall.BoundingBox.Y - height;
                    speed.Y = 0;
                }
            }

            if (myState == PlayerState.Jumping)
            {
                speed.Y += gravity;

                if (speed.Y > fallSpeed)
                    speed.Y = fallSpeed;

                if (CheckCollision(UpperBox))
                {
                    position.Y = collidingWall.BoundingBox.Y + collidingWall.BoundingBox.Height;
                    speed.Y *= -1;
                }
            }
            #endregion

            #region Acceleration
            if (speed.X - 1 < maxSpeed && speed.X + 1 > -maxSpeed)//accelerate player
            {
                if (rightMove && !leftMove)
                {
                    if (speed.X < maxSpeed || speed.X == 0)
                        speed.X += currentAccel;
                    else if (speed.X > -maxSpeed)
                        speed.X -= runningDecelRate;
                }
                if (leftMove && !rightMove)
                {
                    if (speed.X > -maxSpeed || speed.X == 0)
                        speed.X += currentAccel;
                    else if (speed.X < maxSpeed)
                        speed.X += runningDecelRate;
                }
            }

            else if (speed.X > maxSpeed && myState != PlayerState.Boosting)
            {
                speed.X = maxSpeed;
            }
            else if (speed.X < -maxSpeed && myState != PlayerState.Boosting)
            {
                speed.X = -maxSpeed;
            }

            if (currentAccel == 0 && speed.X != 0 && myState != PlayerState.Boosting)
            {
                if (Math.Abs(speed.X) < 1)
                {
                    speed.X = 0;
                }

                else
                {
                    if (myState != PlayerState.Jumping)
                    {
                        if (speed.X > 0)
                            speed.X -= decelRate;
                        else
                            speed.X += decelRate;
                    }
                    else
                    {
                        if (speed.X > 0)
                            speed.X -= 0.4f;
                        else
                            speed.X += 0.4f;
                    }
                }
            }

            #endregion

            #region Collisions
            if (speed.X > 0)
            {
                if (CheckCollision(RightBox))
                {
                    position.X = collidingWall.BoundingBox.X - 60;//60 is the bounding box position x + its width
                    currentAccel = 0;
                }
            }

            else if (speed.X < 0)
            {
                if (CheckCollision(LeftBox))
                {

                    position.X = collidingWall.BoundingBox.X + collidingWall.BoundingBox.Width;
                    currentAccel = 0;
                }
            }
            #endregion
            //Debug.WriteLine(myState);
            previousState = myState;
            previousPosition = position;
            position += speed;
        }

        public void attack()
        {
            lAttack = new LightAttack((int)(position.X), (int)(position.Y), Game1.Instance().getContent(), facing);
        }

        public void attacksToNull()
        {
            //sets all attacks to null
        }

        public override bool CheckCollision(Rectangle collisionBox)
        {
            wallList = LevelConstructor.Instance().getWallList();
            List<Enemy> enemyList = LevelManager.Instance().getEnemyList();
            foreach (Wall wall in wallList)
            {
                if (wall.BoundingBox.Intersects(collisionBox))
                {
                    collidingWall = wall;
                    return true;
                }
            }
            foreach (Enemy enemy in enemyList)
            {
                if (collisionBox.Intersects(enemy.BoundingBox))
                {
                    collidingWall = enemy;
                    currentAccel = 0;
                    return true;
                }
            }
            return false;
        }

        public override void startHitstun(int stunTime)
        {
            myState = PlayerState.Hitstun;
            base.startHitstun(stunTime);
        }

        public override void endHitstun(object sender, ElapsedEventArgs e)
        {
            myState = PlayerState.Idle;
            base.endHitstun(sender, e);
        }

    }
}
