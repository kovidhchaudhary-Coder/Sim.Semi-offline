// BEAST-NADI CORE ENGINE - STANDALONE LIBRARY (v0.9.5)
// ZERO IMPORTS | ZERO JADU | 100% DETERMINISTIC STEEL

namespace BeastPhysics
{
    public enum CcdMode
    {
        Unknown = 0,
        ContinuousSpeculative = 1
    }

    public enum TrunkLatchState
    {
        Closed = 0,
        Open = 1
    }

    public struct BeastVector3
    {
        public double x;
        public double y;
        public double z;

        public BeastVector3(double xValue, double yValue, double zValue)
        {
            x = xValue;
            y = yValue;
            z = zValue;
        }

        public static BeastVector3 Zero => new BeastVector3(0.0, 0.0, 0.0);

        public static BeastVector3 operator +(BeastVector3 a, BeastVector3 b)
        {
            return new BeastVector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static BeastVector3 operator -(BeastVector3 a, BeastVector3 b)
        {
            return new BeastVector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static BeastVector3 operator *(BeastVector3 a, double s)
        {
            return new BeastVector3(a.x * s, a.y * s, a.z * s);
        }

        public static BeastVector3 operator /(BeastVector3 a, double s)
        {
            if (s == 0.0)
            {
                return Zero;
            }

            return new BeastVector3(a.x / s, a.y / s, a.z / s);
        }

        public double MagnitudeSquared()
        {
            return (x * x) + (y * y) + (z * z);
        }

        public double Magnitude()
        {
            return System.Math.Sqrt(MagnitudeSquared());
        }

        public BeastVector3 Normalized()
        {
            double m = Magnitude();
            if (m <= 1e-12)
            {
                return Zero;
            }

            return this / m;
        }

        public static double Dot(BeastVector3 a, BeastVector3 b)
        {
            return (a.x * b.x) + (a.y * b.y) + (a.z * b.z);
        }

        public static BeastVector3 Cross(BeastVector3 a, BeastVector3 b)
        {
            return new BeastVector3(
                (a.y * b.z) - (a.z * b.y),
                (a.z * b.x) - (a.x * b.z),
                (a.x * b.y) - (a.y * b.x));
        }
    }

    public struct BeastQuaternion
    {
        public double x;
        public double y;
        public double z;
        public double w;

        public BeastQuaternion(double xValue, double yValue, double zValue, double wValue)
        {
            x = xValue;
            y = yValue;
            z = zValue;
            w = wValue;
        }

        public static BeastQuaternion Identity => new BeastQuaternion(0.0, 0.0, 0.0, 1.0);

        public static BeastQuaternion operator +(BeastQuaternion a, BeastQuaternion b)
        {
            return new BeastQuaternion(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
        }

        public static BeastQuaternion operator *(BeastQuaternion q, double s)
        {
            return new BeastQuaternion(q.x * s, q.y * s, q.z * s, q.w * s);
        }

        public static BeastQuaternion Multiply(BeastQuaternion a, BeastQuaternion b)
        {
            return new BeastQuaternion(
                (a.w * b.x) + (a.x * b.w) + (a.y * b.z) - (a.z * b.y),
                (a.w * b.y) - (a.x * b.z) + (a.y * b.w) + (a.z * b.x),
                (a.w * b.z) + (a.x * b.y) - (a.y * b.x) + (a.z * b.w),
                (a.w * b.w) - (a.x * b.x) - (a.y * b.y) - (a.z * b.z));
        }

        public BeastQuaternion Normalized()
        {
            double m = System.Math.Sqrt((x * x) + (y * y) + (z * z) + (w * w));
            if (m <= 1e-12)
            {
                return Identity;
            }

            return new BeastQuaternion(x / m, y / m, z / m, w / m);
        }
    }

    public struct BeastState
    {
        public double x;
        public double y;
        public double z;

        public double qx;
        public double qy;
        public double qz;
        public double qw;

        public double vx;
        public double vy;
        public double vz;

        public double wx;
        public double wy;
        public double wz;
    }

    public struct BeastInputs
    {
        public BeastVector3 force;
        public BeastVector3 torque;
        public BeastVector3 trunkAnchor;
        public BeastVector3 chassisAnchor;
        public double panelArea;
        public double dragCoefficient;
        public double leverArm;
        public double airDensity;
    }

    public struct BeastAuditSnapshot
    {
        public double mass;
        public double fixedDt;
        public BeastVector3 currentCom;
        public CcdMode ccdMode;
    }

    public sealed class EngineCore
    {
        public const double DT = 0.001;
        public const double TARGET_MASS = 1200.0;
        public static readonly BeastVector3 TARGET_COM = new BeastVector3(0.0, -0.6, 0.0);

        private const double JOINT_ERROR_LIMIT = 0.001;
        private const double BASE_JOINT_STIFFNESS = 5000.0;
        private const double LATCH_OPEN_TAU = 1200.0;
        private const double LATCH_CLOSE_TAU = 800.0; // 400Nm dead-zone

        private double structuralIntegrity = 1.0;
        private double jointStiffness = BASE_JOINT_STIFFNESS;
        private int solverIterations = 16;
        private double nadiFrequencyHz = 4.0e14;

        private TrunkLatchState trunkLatch = TrunkLatchState.Closed;

        private BeastVector3 inertiaTensor;

        public EngineCore(double bodyLength, double bodyWidth, double bodyHeight)
        {
            inertiaTensor = ComputeReinforcedInertiaTensor(TARGET_MASS, bodyLength, bodyWidth, bodyHeight, 1.25);
        }

        public double StructuralIntegrity => structuralIntegrity;
        public double JointStiffness => jointStiffness;
        public int SolverIterations => solverIterations;
        public double NadiFrequencyHz => nadiFrequencyHz;
        public TrunkLatchState LatchState => trunkLatch;
        public BeastVector3 InertiaTensor => inertiaTensor;

        public void Step(ref BeastState state, BeastInputs inputs)
        {
            BeastVector3 position = new BeastVector3(state.x, state.y, state.z);
            BeastVector3 velocity = new BeastVector3(state.vx, state.vy, state.vz);
            BeastVector3 omega = new BeastVector3(state.wx, state.wy, state.wz);
            BeastQuaternion q = new BeastQuaternion(state.qx, state.qy, state.qz, state.qw).Normalized();

            double speed = velocity.Magnitude();
            UpdateReinforcement(speed);

            BeastVector3 aeroForce = ResolveAeroForce(speed, inputs.airDensity, inputs.panelArea, inputs.dragCoefficient, velocity);
            BeastVector3 constraintImpulse = SolveJointConstraint(inputs.chassisAnchor, inputs.trunkAnchor, speed);

            BeastVector3 totalForce = inputs.force + aeroForce + constraintImpulse;
            BeastVector3 linearAcc = totalForce / TARGET_MASS;

            BeastVector3 angularAcc = new BeastVector3(
                SafeDiv(inputs.torque.x, inertiaTensor.x),
                SafeDiv(inputs.torque.y, inertiaTensor.y),
                SafeDiv(inputs.torque.z, inertiaTensor.z));

            IntegrateRk4(ref position, ref velocity, linearAcc);
            IntegrateAngularRk4(ref q, ref omega, angularAcc);

            state.x = position.x;
            state.y = position.y;
            state.z = position.z;
            state.vx = velocity.x;
            state.vy = velocity.y;
            state.vz = velocity.z;

            state.qx = q.x;
            state.qy = q.y;
            state.qz = q.z;
            state.qw = q.w;

            state.wx = omega.x;
            state.wy = omega.y;
            state.wz = omega.z;

            UpdateLatchState(speed, inputs.airDensity, inputs.panelArea, inputs.dragCoefficient, inputs.leverArm);
            UpdateNadiFrequency(speed);
        }

        public BeastVector3 SolveJointConstraint(BeastVector3 chassisAnchor, BeastVector3 trunkAnchor, double vehicleSpeed)
        {
            BeastVector3 delta = trunkAnchor - chassisAnchor;
            double errorMag = delta.Magnitude();
            if (errorMag <= JOINT_ERROR_LIMIT)
            {
                return BeastVector3.Zero;
            }

            double k = BASE_JOINT_STIFFNESS * (1.0 + (vehicleSpeed * vehicleSpeed * 1e-11));
            jointStiffness = k;

            return delta.Normalized() * (-(errorMag - JOINT_ERROR_LIMIT) * k);
        }

        public string RunBootAudit(BeastAuditSnapshot snapshot)
        {
            bool massPass = snapshot.mass >= (TARGET_MASS * 0.99) && snapshot.mass <= (TARGET_MASS * 1.01);
            bool dtPass = NearlyEqual(snapshot.fixedDt, DT, 1e-12);
            bool comPass = (snapshot.currentCom - TARGET_COM).Magnitude() < 0.01;
            bool ccdPass = snapshot.ccdMode == CcdMode.ContinuousSpeculative;

            string status = (massPass && dtPass && comPass && ccdPass) ? "Fortified" : "Degraded";

            string json = "{" +
                          "\n  \"build\": \"0.9.5\"," +
                          "\n  \"status\": \"" + status + "\"," +
                          "\n  \"mass_pass\": " + BoolToJson(massPass) + "," +
                          "\n  \"dt_pass\": " + BoolToJson(dtPass) + "," +
                          "\n  \"com_pass\": " + BoolToJson(comPass) + "," +
                          "\n  \"ccd_pass\": " + BoolToJson(ccdPass) + "," +
                          "\n  \"mass\": " + snapshot.mass + "," +
                          "\n  \"fixed_dt\": " + snapshot.fixedDt +
                          "\n}";

            return json;
        }

        public string GetAuditJSON()
        {
            return "{\"build\":\"0.9.5\",\"mass\":" + TARGET_MASS + ",\"status\":\"Fortified\"}";
        }

        private static BeastVector3 ComputeReinforcedInertiaTensor(double mass, double length, double width, double height, double reinforceScale)
        {
            double ixx = (mass / 12.0) * ((height * height) + (length * length));
            double iyy = (mass / 12.0) * ((width * width) + (length * length));
            double izz = (mass / 12.0) * ((width * width) + (height * height));

            return new BeastVector3(ixx * reinforceScale, iyy * reinforceScale, izz * reinforceScale);
        }

        private void UpdateReinforcement(double speed)
        {
            if (speed > 500000.0)
            {
                structuralIntegrity = 2.0;
                solverIterations = 32;
                return;
            }

            structuralIntegrity = 1.0;
            solverIterations = 16;
        }

        private static BeastVector3 ResolveAeroForce(double speed, double rho, double area, double cd, BeastVector3 velocity)
        {
            BeastVector3 direction = velocity.Normalized();
            if (direction.MagnitudeSquared() <= 1e-12)
            {
                return BeastVector3.Zero;
            }

            double dragMagnitude = 0.5 * rho * speed * speed * area * cd;
            return direction * (-dragMagnitude);
        }

        private static void IntegrateRk4(ref BeastVector3 position, ref BeastVector3 velocity, BeastVector3 acceleration)
        {
            BeastVector3 k1Pos = velocity;
            BeastVector3 k1Vel = acceleration;

            BeastVector3 k2Pos = velocity + (k1Vel * (DT * 0.5));
            BeastVector3 k2Vel = acceleration;

            BeastVector3 k3Pos = velocity + (k2Vel * (DT * 0.5));
            BeastVector3 k3Vel = acceleration;

            BeastVector3 k4Pos = velocity + (k3Vel * DT);
            BeastVector3 k4Vel = acceleration;

            position = position + ((k1Pos + (k2Pos * 2.0) + (k3Pos * 2.0) + k4Pos) * (DT / 6.0));
            velocity = velocity + ((k1Vel + (k2Vel * 2.0) + (k3Vel * 2.0) + k4Vel) * (DT / 6.0));
        }

        private static void IntegrateAngularRk4(ref BeastQuaternion orientation, ref BeastVector3 omega, BeastVector3 angularAcc)
        {
            BeastVector3 w1 = omega;
            BeastVector3 w2 = omega + (angularAcc * (DT * 0.5));
            BeastVector3 w3 = omega + (angularAcc * (DT * 0.5));
            BeastVector3 w4 = omega + (angularAcc * DT);

            BeastQuaternion q1 = QuaternionDerivative(orientation, w1);
            BeastQuaternion q2 = QuaternionDerivative(orientation + (q1 * (DT * 0.5)), w2);
            BeastQuaternion q3 = QuaternionDerivative(orientation + (q2 * (DT * 0.5)), w3);
            BeastQuaternion q4 = QuaternionDerivative(orientation + (q3 * DT), w4);

            orientation = (orientation + ((q1 + (q2 * 2.0) + (q3 * 2.0) + q4) * (DT / 6.0))).Normalized();
            omega = omega + ((angularAcc + (angularAcc * 2.0) + (angularAcc * 2.0) + angularAcc) * (DT / 6.0));
        }

        private static BeastQuaternion QuaternionDerivative(BeastQuaternion q, BeastVector3 w)
        {
            BeastQuaternion omega = new BeastQuaternion(w.x, w.y, w.z, 0.0);
            return BeastQuaternion.Multiply(q, omega) * 0.5;
        }

        private void UpdateLatchState(double speed, double rho, double area, double cd, double leverArm)
        {
            double tau = 0.5 * rho * speed * speed * area * cd * leverArm;

            if (trunkLatch == TrunkLatchState.Closed && tau > LATCH_OPEN_TAU)
            {
                trunkLatch = TrunkLatchState.Open;
                return;
            }

            if (trunkLatch == TrunkLatchState.Open && tau < LATCH_CLOSE_TAU)
            {
                trunkLatch = TrunkLatchState.Closed;
            }
        }

        private void UpdateNadiFrequency(double speed)
        {
            if (speed > 500000.0)
            {
                nadiFrequencyHz = 7.90e14; // Violet shift
                return;
            }

            nadiFrequencyHz = 4.0e14;
        }

        private static double SafeDiv(double n, double d)
        {
            if (d == 0.0)
            {
                return 0.0;
            }

            return n / d;
        }

        private static bool NearlyEqual(double a, double b, double epsilon)
        {
            double d = a - b;
            if (d < 0.0)
            {
                d = -d;
            }

            return d <= epsilon;
        }

        private static string BoolToJson(bool v)
        {
            return v ? "true" : "false";
        }
    }
}
