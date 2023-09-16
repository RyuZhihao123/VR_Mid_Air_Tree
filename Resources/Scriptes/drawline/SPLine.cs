using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;

namespace SPLINE
{
    class SPLine
    {
        double[] Xi;
        double[] Yi;
        double[] A;
        double[] B;
        double[] C;
        double[] H;
        double[] Lamda;
        double[] Mu;
        double[] G;
        double[] M;
        int N;//
        int n;//
        public SPLine()
        {
            N = 0;
            n = 0;
        }
        public bool Init(double[] Xi, double[] Yi)
        {
            if (Xi.Length != Yi.Length)
                return false;
            if (Xi.Length == 0)
                return false;
            //根据给定的Xi,Yi元素的数目来确定N的大小。
            this.N = Xi.Length;
            n = N - 1;
            //根据实际大小给各个成员变量分配内存
            A = new double[N - 1];
            B = new double[N];
            C = new double[N - 1];
            this.Xi = new double[N];
            this.Yi = new double[N];
            H = new double[N - 1];
            Lamda = new double[N - 1];
            Mu = new double[N - 1];
            G = new double[N];
            M = new double[N];
            //把输入数据点的数值赋给成员变量。
            for (int i = 0; i <= n; i++)
            {
                this.Xi[i] = Xi[i];
                this.Yi[i] = Yi[i];
            }
            /************ 求出hi,Lamda(i),Mu(i),gi *******************/
            GetH();
            GetLamda_Mu_G();
            GetABC();
            /***************** 调用追赶法求出系数矩阵M *********************/
            Chasing chase = new Chasing();
            chase.Init(A, B, C, G);
            chase.Solve(out M);
            return true;
        }
        private void GetH()
        {
            //Get H first;
            for (int i = 0; i <= n - 1; i++)
            {
                H[i] = Xi[i + 1] - Xi[i];
            }
        }
        private void GetLamda_Mu_G()
        {
            double t1, t2;
            for (int i = 1; i <= n - 1; i++)
            {
                Lamda[i] = H[i] / (H[i] + H[i - 1]);
                Mu[i] = 1 - Lamda[i];
                t1 = (Yi[i] - Yi[i - 1]) / H[i - 1];
                t2 = (Yi[i + 1] - Yi[i]) / H[i];
                G[i] = 3 * (Lamda[i] * t1 + Mu[i] * t2);
            }
            G[0] = 3 * (Yi[1] - Yi[0]) / H[0];
            G[n] = 3 * (Yi[n] - Yi[n - 1]) / H[n - 1];
            Mu[0] = 1;
            Lamda[0] = 0;
        }
        private void GetABC()
        {
            for (int i = 1; i <= n - 1; i++)
            {
                A[i - 1] = Lamda[i];
                C[i] = Mu[i];
            }
            A[n - 1] = 1; C[0] = 1;
            for (int i = 0; i <= n; i++)
            {
                B[i] = 2;
            }
        }
        private double fai0(double x)
        {
            double t1, t2;
            double s;
            t1 = 2 * x + 1;
            t2 = (x - 1) * (x - 1);
            s = t1 * t2;
            return s;
        }
        private double fai1(double x)
        {
            double s;
            s = x * (x - 1) * (x - 1);
            return s;
        }
        public double Interpolate(double x)
        {
            double s = 0;
            double P1, P2;
            double t = x;
            int iNum;
            iNum = GetSection(x);
            
            if (iNum == -1) //
            {
                iNum = 0;
                t = Xi[iNum];
                return Yi[0];
            }
            if (iNum == -999)//
            {
                iNum = n - 1;
                t = Xi[iNum + 1];
                return Yi[n];
            }
            P1 = (t - Xi[iNum]) / H[iNum];
            P2 = (Xi[iNum + 1] - t) / H[iNum];
            s = Yi[iNum] * fai0(P1) + Yi[iNum + 1] * fai0(P2) +
            M[iNum] * H[iNum] * fai1(P1) - M[iNum + 1] * H[iNum] * fai1(P2);
            return s;
        }
        private int GetSection(double x)
        {
            int iNum = -1;
            // double EPS = 1.0e-6;
            if (x < Xi[0])
            {
                return -1;
            }
            if (x > Xi[N - 1])
            {
                return -999;
            }
            for (int i = 0; i <= n - 1; i++)
            {
                if (x >= Xi[i] && x <= Xi[i + 1])
                {
                    iNum = i;
                    break;
                }
            }
            return iNum;
        }
    }

    class Chasing
    {
        protected int N;//Dimension of Martrix Ax=d;
        protected double[] d;//Ax=d;
        protected double[] Aa;//a in A;
        protected double[] Ab; //b in A:
        protected double[] Ac;// c in A;
        protected double[] L;//LU-->L;
        protected double[] U;//LU-->U;
        public double[] S;//store the result;
                          //constructor without parameters;
        public Chasing()
        {
        }
        public bool Init(double[] a, double[] b, double[] c, double[] d)
        {
            //check validation of dimentions;
            int na = a.Length;
            int nb = b.Length;
            int nc = c.Length;
            int nd = d.Length;
            if (nb < 3)
                return false;
            N = nb;
            if (na != N - 1 || nc != N - 1 || nd != N)
                return false;
            S = new double[N];
            L = new double[N - 1];
            U = new double[N];
            Aa = new double[N - 1];
            Ab = new double[N];
            Ac = new double[N - 1];
            this.d = new double[N];
            //init Aa,Ab,Ac,Ad;
            for (int i = 0; i <= N - 2; i++)
            {
                Ab[i] = b[i];
                this.d[i] = d[i];
                Aa[i] = a[i];
                Ac[i] = c[i];
            }
            Ab[N - 1] = b[N - 1];
            this.d[N - 1] = d[N - 1];
            return true;
        }
        public bool Solve(out double[] R)
        {
            R = new double[Ab.Length];
            /*********************A=LU***********************************/
            U[0] = Ab[0];
            for (int i = 2; i <= N; i++)
            {
                // L[i] = Aa[i] / U[i - 1];
                L[i - 2] = Aa[i - 2] / U[i - 2];
                //U[i]=Ab[i]-Ac[i-1]*L[i];
                U[i - 1] = Ab[i - 1] - Ac[i - 2] * L[i - 2];
            }
            /*************************END of A=LU **********************/
            /**************** Ly=d ******************************/
            double[] Y = new double[d.Length];
            Y[0] = d[0];
            for (int i = 2; i <= N; i++)
            {
                //Y[k]=d[k]-L[k]*Y[k-1];
                Y[i - 1] = d[i - 1] - (L[i - 2]) * (Y[i - 2]);
            }
            /**************** End of Ly=d ****************************/
            /*************** Ux=Y ********************************/
            //X[n]=Y[n]/U[n];
            R[N - 1] = Y[N - 1] / U[N - 1];
            //X[k]=(Y[k]-C[k]*X[k+1])/U[k];(n-1,,.....1)
            for (int i = N - 1; i >= 1; i--)
            {
                R[i - 1] = (Y[i - 1] - Ac[i - 1] * R[i]) / U[i - 1];
            }
            /*************** End of Ux=Y *************************/
            for (int i = 0; i < R.Length; i++)
            {
                if (double.IsInfinity(R[i]) || double.IsNaN(R[i]))
                    return false;
            }
            return true;
        }
    }
}

